# AI Chat Frontend Integration - Architecture & Implementation Guide

## Overview
This document describes the design and implementation of a **stunning AI-powered chat frontend** that integrates with the existing Business Analytics backend. The frontend provides a conversational interface for managing projects, tasks, users, and reports through natural language.

## Key Principles
- **Zero Breaking Changes**: No modifications to existing backend APIs, database, or business logic
- **Additive Only**: New frontend, new API endpoints (optional), new integration layer
- **AI-First UX**: Natural language commands for all CRUD operations
- **Real-time**: WebSocket updates for collaborative editing
- **Secure**: Inherits all existing auth/authorization rules

---

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                            EXISTING SYSTEM (UNCHANGED)                      │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │
│  │   Domain     │  │ Application  │  │ Infrastructure│  │     API      │   │
│  │  (Entities,  │  │  (Services,  │  │  (EF Core,   │  │ (Controllers,│   │
│  │  Enums,      │  │  DTOs,       │  │  Repos,      │  │  Middleware, │   │
│  │  Rules)      │  │  Validators) │  │  SQL Reports)│  │  Auth)       │   │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘   │
│         ▲               ▲               ▲               ▲                  │
│         └───────────────┴───────────────┴───────────────┘                  │
│                              │                                             │
│                    ┌─────────┴─────────┐                                   │
│                    │  PostgreSQL 16    │                                   │
│                    └───────────────────┘                                   │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │  REST API (JWT Auth)
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         NEW AI FRONTEND LAYER                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐     │
│  │  React/Next.js   │    │  AI Integration  │    │  WebSocket       │     │
│  │  Frontend        │◄───│  Service         │───►│  Server          │     │
│  │  (TypeScript)    │    │  (Orchestrator)  │    │  (Real-time)     │     │
│  └──────────────────┘    └──────────────────┘    └──────────────────┘     │
│         │                       │                       │                  │
│         │                       ▼                       │                  │
│         │              ┌──────────────────┐             │                  │
│         │              │  LLM Provider    │             │                  │
│         │              │  (OpenAI/Anthropic│             │                  │
│         │              │   /Local)        │             │                  │
│         │              └──────────────────┘             │                  │
│         │                       │                       │                  │
│         ▼                       ▼                       ▼                  │
│  ┌────────────────────────────────────────────────────────────────────┐   │
│  │                    AI Chat Interface                                │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌──────────┐  │   │
│  │  │ Conversation│  │ Context     │  │ Action      │  │ Preview  │  │   │
│  │  │ History     │  │ Manager     │  │ Executor    │  │ Panel    │  │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  └──────────┘  │   │
│  └────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## AI Integration Layer (New - Additive Only)

### 1. AI Orchestration Service
```typescript
// New service: src/ai/AIOrchestrator.ts
interface AIOrchestrator {
  // Parse natural language to structured intent
  parseIntent(message: string, context: ConversationContext): Promise<Intent>;
  
  // Execute intent via existing API
  executeIntent(intent: Intent, authToken: string): Promise<ActionResult>;
  
  // Generate response with preview
  generateResponse(result: ActionResult): Promise<AIResponse>;
  
  // Handle clarification dialogs
  askClarification(ambiguous: Intent): Promise<Clarification>;
}
```

### 2. Intent Classification
```typescript
// Maps natural language to existing API operations
type IntentType = 
  // Project operations
  | 'create_project' | 'update_project' | 'delete_project' | 'get_project' | 'list_projects'
  | 'complete_project' | 'reopen_project'
  
  // Task operations  
  | 'create_task' | 'update_task' | 'delete_task' | 'get_task' | 'list_tasks'
  | 'assign_task' | 'complete_task' | 'reopen_task' | 'block_task'
  
  // User operations
  | 'create_user' | 'update_user' | 'deactivate_user' | 'get_user' | 'list_users'
  
  // Department operations
  | 'create_department' | 'update_department' | 'deactivate_department' | 'list_departments'
  
  // Reports & Analytics
  | 'get_department_progress' | 'get_project_summary' | 'export_report'
  
  // System
  | 'health_check' | 'backup' | 'restore';

interface Intent {
  type: IntentType;
  entities: Record<string, any>;      // Extracted parameters
  confidence: number;                  // 0-1
  requiresConfirmation: boolean;       // Destructive actions
  missingFields: string[];             // For clarification
}
```

### 3. Context Manager
```typescript
// Maintains conversation state and entity references
interface ConversationContext {
  // Current focus entities
  activeProject?: ProjectRef;
  activeTask?: TaskRef;
  activeUser?: UserRef;
  activeDepartment?: DepartmentRef;
  
  // Conversation history
  history: Message[];
  
  // User permissions (from JWT)
  userRole: UserRole;
  userDepartment: string;
  userId: string;
  
  // Pending actions awaiting confirmation
  pendingAction?: PendingAction;
}
```

---

## Frontend Architecture (New)

### Tech Stack
- **Framework**: Next.js 14 (App Router) + React 18
- **Language**: TypeScript (strict mode)
- **Styling**: Tailwind CSS + shadcn/ui + Framer Motion
- **State**: Zustand (global) + React Query (server state)
- **Real-time**: Socket.io client
- **AI**: Vercel AI SDK (streaming responses)
- **Charts**: Recharts / Tremor
- **Forms**: React Hook Form + Zod

### Project Structure (New - `/frontend`)
```
frontend/
├── src/
│   ├── app/                    # Next.js App Router
│   │   ├── (auth)/            # Auth layout group
│   │   │   ├── login/
│   │   │   └── register/
│   │   ├── (dashboard)/       # Protected dashboard layout
│   │   │   ├── chat/          # AI Chat interface (MAIN)
│   │   │   ├── projects/      # Traditional UI (optional)
│   │   │   ├── tasks/
│   │   │   ├── reports/
│   │   │   ├── users/
│   │   │   └── settings/
│   │   ├── api/               # Next.js API routes (proxy)
│   │   │   ├── auth/
│   │   │   ├── chat/          # AI chat streaming endpoint
│   │   │   └── proxy/         # Backend API proxy
│   │   ├── layout.tsx
│   │   └── page.tsx           # Landing → redirects to /chat
│   │
│   ├── components/
│   │   ├── chat/              # Chat-specific components
│   │   │   ├── ChatInterface.tsx
│   │   │   ├── MessageBubble.tsx
│   │   │   ├── ActionPreview.tsx
│   │   │   ├── ClarificationDialog.tsx
│   │   │   ├── ContextPanel.tsx
│   │   │   ├── StreamingResponse.tsx
│   │   │   └── CommandPalette.tsx
│   │   ├── ui/                # shadcn/ui components
│   │   ├── layout/            # Sidebar, Header, Breadcrumbs
│   │   ├── data-display/      # Tables, Charts, Cards
│   │   └── forms/             # Form components
│   │
│   ├── lib/
│   │   ├── ai/                # AI Integration
│   │   │   ├── orchestrator.ts
│   │   │   ├── intent-parser.ts
│   │   │   ├── context-manager.ts
│   │   │   ├── action-executor.ts
│   │   │   └── prompt-templates.ts
│   │   ├── api/               # API client
│   │   │   ├── client.ts      # Axios instance with auth
│   │   │   ├── endpoints.ts   # Typed API endpoints
│   │   │   └── websocket.ts   # Socket.io connection
│   │   ├── auth/              # Auth utilities
│   │   ├── store/             # Zustand stores
│   │   ├── hooks/             # Custom React hooks
│   │   └── utils/             # Helpers
│   │
│   ├── types/                 # TypeScript types
│   │   ├── api.ts             # Backend API types (generated)
│   │   ├── chat.ts            # Chat types
│   │   └── ai.ts              # AI types
│   │
│   └── styles/
│       └── globals.css        # Tailwind + custom
│
├── public/                    # Static assets
├── tests/                     # Playwright E2E tests
├── .env.local                 # Local env (gitignored)
├── next.config.js
├── tailwind.config.ts
├── tsconfig.json
└── package.json
```

---

## AI Chat Interface Design

### Main Chat View (`/chat`)
```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Header: Logo | Search/Command Palette (⌘K) | User Menu | Theme Toggle    │
├──────────────────┬──────────────────────────────────────────────────────────┤
│                  │                                                          │
│  SIDEBAR         │  MAIN CHAT AREA                                          │
│  ──────────────  │  ────────────────────                                   │
│                  │                                                          │
│  📁 Projects     │  ┌──────────────────────────────────────────────────┐   │
│    🟢 API Refactor│  │  AI Assistant                          🔄 📋 │   │
│    🟡 DB Migration│  ├──────────────────────────────────────────────────┤   │
│    🔵 Security    │  │                                                │   │
│                  │  │  🤖 Hello! I can help you manage projects,       │   │
│  👥 Team         │  │  tasks, users, and reports. Try saying:         │   │
│    @john (Admin) │  │  • "Create a project called Website Redesign"    │   │
│    @jane (Mgr)   │  │  • "Show me Engineering department progress"     │   │
│    @bob (Dev)    │  │  • "Assign task #123 to @jane"                   │   │
│                  │  │  • "Export Q4 report as CSV"                     │   │
│  📊 Reports      │  │                                                │   │
│    📈 Dept Progress│  │  👤 You: Create a new project for Q1 Planning  │   │
│    📋 Export     │  │                                                │   │
│                  │  │  🤖 I'll help you create a project.              │   │
│  ⚙️ Settings    │  │  ┌────────────────────────────────────────────┐  │   │
│                  │  │  │ ACTION PREVIEW                              │  │   │
│  Context:        │  │  │ Type: Create Project                       │  │   │
│  🏢 Engineering  │  │  │ Name: Q1 Planning                          │  │   │
│  👤 @john (Admin)│  │  │ Department: Engineering (your dept)        │  │   │
│  🔑 Admin        │  │  │ Owner: You (@john)                         │  │   │
│                  │  │  │ Status: Planned                            │  │   │
│  [New Chat]      │  │  │ Start: 2024-01-15 | Due: 2024-03-31       │  │   │
│                  │  │  └────────────────────────────────────────────┘  │   │
│                  │  │                                                │   │
│                  │  │  🤖 Should I create this project? [Confirm]    │   │
│                  │  │                                                │   │
│                  │  ├──────────────────────────────────────────────────┤   │
│                  │  │  💬 Type a message...                          │ ⬆ │   │
│                  │  └──────────────────────────────────────────────────┘   │
│                  │                                                          │
└──────────────────┴──────────────────────────────────────────────────────────┘
```

### Key Chat Components

#### 1. Message Bubble with Action Preview
```tsx
// components/chat/MessageBubble.tsx
interface MessageBubbleProps {
  message: ChatMessage;
  onConfirm?: (actionId: string) => void;
  onReject?: (actionId: string) => void;
}

// Renders differently based on message type:
// - User message: right-aligned, simple
// - AI response: left-aligned, with streaming
// - Action preview: interactive card with Confirm/Edit/Cancel
// - Clarification: form fields for missing parameters
// - Result: success/error with data preview
```

#### 2. Streaming Response
```tsx
// Uses Vercel AI SDK useChat hook
const { messages, input, handleInputChange, handleSubmit, isLoading } = useChat({
  api: '/api/chat',
  initialMessages: [],
  onFinish: (message) => {
    // Handle action execution after AI response
  }
});
```

#### 3. Command Palette (⌘K)
```tsx
// Quick actions without typing
const commands = [
  { label: 'New Project', icon: 'plus', action: 'create_project' },
  { label: 'Department Progress', icon: 'chart', action: 'get_department_progress' },
  { label: 'Export Report', icon: 'download', action: 'export_report' },
  { label: 'Create Task', icon: 'check-square', action: 'create_task' },
  { label: 'Assign User', icon: 'user-plus', action: 'assign_task' },
];
```

#### 3. Context Panel (Sidebar)
- Shows current focus (project, task, user)
- Quick stats for active entity
- One-click actions
- Breadcrumb navigation

---

## Prompt Engineering for AI

### System Prompt Template
```typescript
// lib/ai/prompt-templates.ts
const SYSTEM_PROMPT = `
You are an AI assistant for the Business Analytics platform.
You help users manage projects, tasks, users, departments, and reports through natural language.

AVAILABLE ACTIONS (map to existing REST API):
${ACTIONS.map(a => `- ${a.name}: ${a.description} [${a.requiredParams.join(', ')}]`).join('\n')}

CURRENT USER CONTEXT:
- Role: {{userRole}} (Admin/Manager/Contributor/Viewer)
- Department: {{userDepartment}}
- User ID: {{userId}}

RULES:
1. ALWAYS confirm before destructive actions (delete, deactivate)
2. Ask for missing required parameters
3. Respect department scoping - users can only access their department
4. Use department context for defaults
5. Show action preview before execution
4. Reference existing entities by name when possible
5. Never hallucinate - if unsure, ask for clarification

RESPONSE FORMAT:
- If action needed: Show preview card, ask for confirmation
- If clarification needed: Ask specific questions
- If info request: Provide data with context
- If error: Explain clearly with recovery steps
`;
```

### Example Interactions

**Create Project:**
```
User: "Create a project called Mobile App Redesign for Engineering, due March 31st"
AI: Shows preview card with:
    - Name: Mobile App Redesign
    - Department: Engineering (from context)
    - Owner: You
    - Status: Planned
    - Due: 2024-03-31
    [Confirm] [Edit] [Cancel]
User: "Confirm"
AI: Executes POST /api/v1/projects, returns project details
```

**Department Report:**
```
User: "Show me Engineering progress and export as CSV"
AI: Executes GET /api/v1/reports/departments/progress?departmentId=eng
    Shows formatted report in chat
    Asks: "Should I also export this as CSV?"
User: "Yes"
AI: Executes export endpoint, provides download link
```

**Task Assignment:**
```
User: "Assign the API authentication task to @jane"
AI: Resolves "API authentication task" → finds task in context
    Resolves "@jane" → finds user in department
    Shows preview: Task #123 → @jane (Engineering Manager)
    [Confirm]
User: "Confirm"
AI: Executes PATCH /api/v1/tasks/123/assign
```

---

## Integration with Existing Backend (Additive Only)

### Optional: New API Endpoints (Backend - Additive)
```csharp
// New controller: AiChatController.cs (OPTIONAL - only if needed)
[ApiController]
[Route("api/v1/ai")]
[Authorize]
public class AiChatController : ControllerBase
{
    // Streaming chat endpoint for frontend
    [HttpPost("chat/stream")]
    public async Task ChatStream([FromBody] ChatRequest request, CancellationToken ct)
    {
        // Proxy to AI service, stream response
        // Can access existing services for context
    }
    
    // Batch operations for complex intents
    [HttpPost("batch")]
    public async Task<BatchResult> ExecuteBatch([FromBody] BatchRequest request)
    {
        // Execute multiple operations atomically
    }
    
    // Get context for AI (projects, users in dept)
    [HttpGet("context")]
    public async Task<AiContext> GetContext()
    {
        // Returns lightweight context for AI
    }
}
```

### Frontend API Proxy (Next.js)
```typescript
// app/api/proxy/[...path]/route.ts
export async function POST(request: Request, { params }: { params: { path: string[] } }) {
  const token = getTokenFromRequest(request);
  const backendUrl = process.env.BACKEND_URL || 'http://localhost:8080';
  
  const response = await fetch(`${backendUrl}/api/v1/${params.path.join('/')}`, {
    method: request.method,
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
    body: request.body,
  });
  
  return new Response(response.body, {
    status: response.status,
    headers: response.headers,
  });
}
```

---

## Real-time Collaboration (WebSocket)

### Socket.io Events
```typescript
// Shared types
interface ServerToClientEvents {
  'entity:updated': (data: EntityUpdate) => void;
  'entity:deleted': (data: EntityDelete) => void;
  'report:generated': (data: ReportGenerated) => void;
  'user:presence': (data: UserPresence) => void;
  'chat:message': (data: ChatMessage) => void;
}

interface ClientToServerEvents {
  'subscribe': (entityType: string, entityId: string) => void;
  'unsubscribe': (entityType: string, entityId: string) => void;
  'chat:send': (message: ChatMessage) => void;
  'presence:update': (status: PresenceStatus) => void;
}
```

---

## Testing Strategy (New)

### 1. Unit Tests (AI Layer)
```typescript
// tests/ai/orchestrator.test.ts
describe('AIOrchestrator', () => {
  it('parses create project intent', async () => {
    const intent = await orchestrator.parseIntent(
      'Create project called Test for Engineering due next month',
      context
    );
    expect(intent.type).toBe('create_project');
    expect(intent.entities.name).toBe('Test');
  });
  
  it('asks clarification for missing params', async () => {
    const intent = await orchestrator.parseIntent('Create a project', context);
    expect(intent.missingFields).toContain('name');
  });
});
```

### 2. Integration Tests (Chat Flow)
```typescript
// tests/chat/integration.test.ts
test('complete create project flow', async () => {
  // 1. User sends message
  // 2. AI parses intent
  // 3. Shows preview
  // 4. User confirms
  // 5. API called
  // 6. Result displayed
});
```

### 3. E2E Tests (Playwright)
```typescript
// tests/e2e/chat.spec.ts
test('AI creates project via chat', async ({ page }) => {
  await page.goto('/chat');
  await page.fill('[data-testid=chat-input]', 'Create project called Test for Engineering');
  await page.press('[data-testid=chat-input]', 'Enter');
  await expect(page.locator('[data-testid=action-preview]')).toBeVisible();
  await page.click('[data-testid=confirm-action]');
  await expect(page.locator('[data-testid=result-success]')).toBeVisible();
});
```

---

## Deployment (Additive)

### Docker Compose Addition
```yaml
# docker-compose.frontend.yml (extends base)
services:
  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
    ports:
      - "3000:3000"
    environment:
      - BACKEND_URL=http://api:8080
      - OPENAI_API_KEY=${OPENAI_API_KEY}
      - NEXTAUTH_SECRET=${NEXTAUTH_SECRET}
    depends_on:
      - api
    networks:
      - business-analytics-network

  websocket:
    build:
      context: ./websocket-server
      dockerfile: Dockerfile
    ports:
      - "3001:3001"
    environment:
      - REDIS_URL=redis://redis:6379
    depends_on:
      - redis
    networks:
      - business-analytics-network

  redis:
    image: redis:7-alpine
    volumes:
      - redis_data:/data
    networks:
      - business-analytics-network
```

### Environment Variables (New)
```env
# Frontend
NEXT_PUBLIC_API_URL=http://localhost:8080
NEXT_PUBLIC_WS_URL=ws://localhost:3001
OPENAI_API_KEY=sk-...
ANTHROPIC_API_KEY=sk-ant-...
NEXTAUTH_SECRET=...
NEXTAUTH_URL=http://localhost:3000

# AI Model Selection
AI_PROVIDER=openai  # or anthropic, ollama
AI_MODEL=gpt-4-turbo-preview
AI_TEMPERATURE=0.3
```

---

## Rollout Plan

### Phase 1: Core Chat (Week 1-2)
- [ ] Next.js project setup with Tailwind + shadcn/ui
- [ ] Basic chat interface with streaming
- [ ] Intent parser for top 10 actions
- [ ] Connect to existing API via proxy
- [ ] Auth integration (JWT)

### Phase 2: Action Execution (Week 2-3)
- [ ] Action preview cards
- [ ] Confirmation dialogs
- [ ] All CRUD operations mapped
- [ ] Context management
- [ ] Error handling

### Phase 3: Advanced Features (Week 3-4)
- [ ] Command palette (⌘K)
- [ ] Report generation + export
- [ ] Clarification dialogs
- [ ] WebSocket real-time updates
- [ ] Presence indicators

### Phase 4: Polish & Production (Week 4-5)
- [ ] Accessibility (WCAG 2.1 AA)
- [ ] Performance optimization
- [ ] E2E tests (Playwright)
- [ ] Documentation
- [ ] Deploy to staging → production

---

## Security Considerations

1. **AI never executes directly** - All actions go through existing API with JWT
2. **Department scoping enforced** - Backend validates, AI only suggests
3. **Confirmation required** - Destructive actions need explicit user approval
4. **Audit trail** - All AI-initiated actions logged via existing audit system
5. **Rate limiting** - Frontend + backend rate limits on chat endpoint
6. **PII protection** - No sensitive data sent to LLM without user consent
7. **Model fallback** - Local models (Ollama) for sensitive environments

---

## Files to Create (New Only)

| File | Purpose |
|------|---------|
| `frontend/package.json` | Dependencies |
| `frontend/next.config.js` | Next.js config |
| `frontend/tailwind.config.ts` | Tailwind config |
| `frontend/tsconfig.json` | TypeScript config |
| `frontend/src/app/layout.tsx` | Root layout |
| `frontend/src/app/(dashboard)/chat/page.tsx` | Main chat page |
| `frontend/src/components/chat/ChatInterface.tsx` | Main chat component |
| `frontend/src/components/chat/MessageBubble.tsx` | Message rendering |
| `frontend/src/components/chat/ActionPreview.tsx` | Action confirmation |
| `frontend/src/lib/ai/orchestrator.ts` | AI orchestration |
| `frontend/src/lib/ai/intent-parser.ts` | NLP parsing |
| `frontend/src/lib/ai/context-manager.ts` | Conversation state |
| `frontend/src/lib/ai/action-executor.ts` | API execution |
| `frontend/src/lib/api/client.ts` | Axios API client |
| `frontend/src/lib/store/chat-store.ts` | Zustand chat store |
| `frontend/src/types/chat.ts` | Chat TypeScript types |
| `frontend/Dockerfile` | Frontend container |
| `docker-compose.frontend.yml` | Frontend services |
| `.github/workflows/frontend-ci.yml` | Frontend CI |

---

## Summary

This design provides a **stunning, AI-powered chat frontend** that:
- ✅ **Zero changes** to existing backend
- ✅ **Full CRUD via chat** for all entities
- ✅ **Real-time collaboration** via WebSocket
- ✅ **Secure by default** - inherits all auth rules
- ✅ **Extensible** - easy to add new intents
- ✅ **Production-ready** - Docker, CI/CD, monitoring
- ✅ **Accessible** - WCAG 2.1 AA compliant
- ✅ **Testable** - Unit, integration, E2E coverage

The frontend lives in `/frontend` and deploys alongside the existing backend via `docker-compose.frontend.yml`.