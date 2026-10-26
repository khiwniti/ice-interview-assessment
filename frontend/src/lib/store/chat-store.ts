import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { ChatMessage, ConversationContext, Intent, ActionPreview, CommandPaletteItem } from '@/types/chat';

interface ChatState {
  // Messages
  messages: ChatMessage[];
  addMessage: (message: ChatMessage) => void;
  updateMessage: (id: string, updates: Partial<ChatMessage>) => void;
  clearMessages: () => void;
  
  // Conversation context
  context: ConversationContext;
  setContext: (context: Partial<ConversationContext>) => void;
  
  // Pending action
  pendingAction: ActionPreview | null;
  setPendingAction: (action: ActionPreview | null) => void;
  
  // UI state
  isStreaming: boolean;
  setIsStreaming: (streaming: boolean) => void;
  isSidebarOpen: boolean;
  setSidebarOpen: (open: boolean) => void;
  
  // Command palette
  commandPaletteOpen: boolean;
  setCommandPaletteOpen: (open: boolean) => void;
}

export const useChatStore = create<ChatState>()(
  persist(
    (set) => ({
      messages: [],
      addMessage: (message) => set((state) => ({ 
        messages: [...state.messages, message] 
      })),
      updateMessage: (id, updates) => set((state) => ({
        messages: state.messages.map(m => m.id === id ? { ...m, ...updates } : m)
      })),
      clearMessages: () => set({ messages: [] }),
      
      context: {
        history: [],
        userRole: 'Viewer',
        userDepartment: '',
        userId: '',
      },
      setContext: (context) => set((state) => ({
        context: { ...state.context, ...context }
      })),
      
      pendingAction: null,
      setPendingAction: (action) => set({ pendingAction: action }),
      
      isStreaming: false,
      setIsStreaming: (streaming) => set({ isStreaming: streaming }),
      
      isSidebarOpen: true,
      setSidebarOpen: (open) => set({ isSidebarOpen: open }),
      
      commandPaletteOpen: false,
      setCommandPaletteOpen: (open) => set({ commandPaletteOpen: open }),
    }),
    {
      name: 'chat-storage',
      partialize: (state) => ({
        messages: state.messages.slice(-50), // Keep last 50 messages
        context: state.context,
      }),
    }
  )
);

// Command palette items
export const COMMAND_PALETTE_ITEMS: CommandPaletteItem[] = [
  { label: 'New Project', description: 'Create a new project', icon: 'Plus', action: 'create_project', category: 'project' },
  { label: 'New Task', description: 'Create a new task', icon: 'CheckSquare', action: 'create_task', category: 'task' },
  { label: 'Department Progress', description: 'View department progress report', icon: 'BarChart', action: 'get_department_progress', category: 'report' },
  { label: 'Project Summary', description: 'View project summary report', icon: 'FileText', action: 'get_project_summary', category: 'report' },
  { label: 'Export Report', description: 'Export report as JSON or CSV', icon: 'Download', action: 'export_report', category: 'report' },
  { label: 'New User', description: 'Create a new user', icon: 'UserPlus', action: 'create_user', category: 'user' },
  { label: 'New Department', description: 'Create a new department', icon: 'Building', action: 'create_department', category: 'user' },
  { label: 'Health Check', description: 'Check system health', icon: 'Activity', action: 'health_check', category: 'system' },
  { label: 'Backup Database', description: 'Create database backup', icon: 'Database', action: 'backup', category: 'system' },
];