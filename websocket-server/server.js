const { Server } = require('socket.io');
const { createClient } = require('redis');
const jwt = require('jsonwebtoken');
require('dotenv').config();

const PORT = process.env.PORT || 3001;
const REDIS_URL = process.env.REDIS_URL || 'redis://redis:6379';
const JWT_SECRET = process.env.JWT_SECRET || 'your-jwt-secret';

const io = new Server(PORT, {
  cors: {
    origin: process.env.FRONTEND_URL || 'http://localhost:3000',
    methods: ['GET', 'POST'],
    credentials: true,
  },
});

const redis = createClient({ url: REDIS_URL });
redis.on('error', (err) => console.error('Redis error:', err));
redis.connect().catch(console.error);

// Store active connections
const connections = new Map(); // socketId -> { userId, userRole, department, subscriptions }

// JWT verification
function verifyToken(token) {
  try {
    return jwt.verify(token, JWT_SECRET);
  } catch (e) {
    return null;
  }
}

// Socket authentication middleware
io.use(async (socket, next) => {
  const token = socket.handshake.auth.token || socket.handshake.headers.authorization?.replace('Bearer ', '');
  
  if (!token) {
    return next(new Error('Authentication required'));
  }

  const decoded = verifyToken(token);
  if (!decoded) {
    return next(new Error('Invalid token'));
  }

  socket.user = decoded;
  next();
});

io.on('connection', (socket) => {
  console.log(`Client connected: ${socket.id} (user: ${socket.user.sub})`);

  // Store connection info
  connections.set(socket.id, {
    userId: socket.user.sub,
    userRole: socket.user.role,
    department: socket.user.department,
    subscriptions: new Set(),
  });

  // Join user's personal room
  socket.join(`user:${socket.user.sub}`);

  // Join department room
  if (socket.user.department) {
    socket.join(`department:${socket.user.department}`);
  }

  // Handle entity subscriptions
  socket.on('subscribe', async (data) => {
    const { entityType, entityId } = data;
    const room = `${entityType}:${entityId}`;
    socket.join(room);
    
    const conn = connections.get(socket.id);
    if (conn) {
      conn.subscriptions.add(room);
    }
    
    console.log(`Socket ${socket.id} subscribed to ${room}`);
  });

  // Handle entity unsubscriptions
  socket.on('unsubscribe', (data) => {
    const { entityType, entityId } = data;
    const room = `${entityType}:${entityId}`;
    socket.leave(room);
    
    const conn = connections.get(socket.id);
    if (conn) {
      conn.subscriptions.delete(room);
    }
    
    console.log(`Socket ${socket.id} unsubscribed from ${room}`);
  });

  // Handle chat messages (broadcast to room)
  socket.on('chat:message', (data) => {
    const { room, message } = data;
    socket.to(room).emit('chat:message', {
      ...message,
      senderId: socket.user.sub,
      timestamp: new Date().toISOString(),
    });
  });

  // Handle presence updates
  socket.on('presence:update', (status) => {
    const conn = connections.get(socket.id);
    if (conn) {
      conn.status = status;
    }
    
    // Broadcast to department
    if (socket.user.department) {
      socket.to(`department:${socket.user.department}`).emit('user:presence', {
        userId: socket.user.sub,
        status,
        timestamp: new Date().toISOString(),
      });
    }
  });

  // Handle typing indicators
  socket.on('typing:start', (room) => {
    socket.to(room).emit('user:typing', { userId: socket.user.sub });
  });

  socket.on('typing:stop', (room) => {
    socket.to(room).emit('user:typing:stop', { userId: socket.user.sub });
  });

  // Handle disconnect
  socket.on('disconnect', () => {
    const conn = connections.get(socket.id);
    if (conn) {
      // Notify department of user offline
      if (socket.user.department) {
        socket.to(`department:${socket.user.department}`).emit('user:presence', {
          userId: socket.user.sub,
          status: 'offline',
          timestamp: new Date().toISOString(),
        });
      }
    }
    connections.delete(socket.id);
    console.log(`Client disconnected: ${socket.id}`);
  });
});

// Redis subscriber for cross-server events
async function setupRedisSubscriber() {
  const subscriber = redis.duplicate();
  await subscriber.connect();

  // Subscribe to entity update channel
  await subscriber.subscribe('entity:updates', (message) => {
    try {
      const data = JSON.parse(message);
      io.to(`${data.entityType}:${data.entityId}`).emit('entity:updated', data);
    } catch (e) {
      console.error('Failed to parse entity update:', e);
    }
  });

  await subscriber.subscribe('entity:deleted', (message) => {
    try {
      const data = JSON.parse(message);
      io.to(`${data.entityType}:${data.entityId}`).emit('entity:deleted', data);
    } catch (e) {
      console.error('Failed to parse entity delete:', e);
    }
  });

  await subscriber.subscribe('report:generated', (message) => {
    try {
      const data = JSON.parse(message);
      io.to(`department:${data.departmentId}`).emit('report:generated', data);
    } catch (e) {
      console.error('Failed to parse report generated:', e);
    }
  });

  console.log('Redis subscriber connected');
}

setupRedisSubscriber().catch(console.error);

// Health check endpoint
const http = require('http');
const healthServer = http.createServer((req, res) => {
  if (req.url === '/health') {
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ status: 'healthy', connections: connections.size }));
  } else {
    res.writeHead(404);
    res.end();
  }
});

healthServer.listen(3002, () => {
  console.log(`Health check server running on port 3002`);
});

console.log(`WebSocket server running on port ${PORT}`);