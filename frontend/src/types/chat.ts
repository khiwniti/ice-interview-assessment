export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp: Date;
  type?: 'text' | 'action_preview' | 'clarification' | 'result' | 'error';
  metadata?: {
    actionId?: string;
    intentType?: string;
    entities?: Record<string, any>;
    previewData?: any;
    resultData?: any;
    errorData?: any;
    clarificationFields?: ClarificationField[];
  };
}

export interface ClarificationField {
  key: string;
  label: string;
  type: 'text' | 'select' | 'date' | 'user' | 'project' | 'department';
  required: boolean;
  options?: { value: string; label: string }[];
  value?: any;
}

export interface ConversationContext {
  activeProject?: EntityRef;
  activeTask?: EntityRef;
  activeUser?: EntityRef;
  activeDepartment?: EntityRef;
  history: ChatMessage[];
  userRole: UserRole;
  userDepartment: string;
  userId: string;
  pendingAction?: PendingAction;
}

export interface EntityRef {
  id: string;
  name: string;
  type: 'project' | 'task' | 'user' | 'department';
}

export interface PendingAction {
  actionId: string;
  intentType: IntentType;
  entities: Record<string, any>;
  previewData: any;
  requiresConfirmation: boolean;
}

export type UserRole = 'Admin' | 'Manager' | 'Contributor' | 'Viewer';

export type IntentType = 
  | 'create_project' | 'update_project' | 'delete_project' | 'get_project' | 'list_projects'
  | 'complete_project' | 'reopen_project'
  | 'create_task' | 'update_task' | 'delete_task' | 'get_task' | 'list_tasks'
  | 'assign_task' | 'complete_task' | 'reopen_task' | 'block_task'
  | 'create_user' | 'update_user' | 'deactivate_user' | 'get_user' | 'list_users'
  | 'create_department' | 'update_department' | 'deactivate_department' | 'list_departments'
  | 'get_department_progress' | 'get_project_summary' | 'export_report'
  | 'health_check' | 'backup' | 'restore';

export interface Intent {
  type: IntentType;
  entities: Record<string, any>;
  confidence: number;
  requiresConfirmation: boolean;
  missingFields: string[];
  originalMessage: string;
}

export interface ActionResult {
  success: boolean;
  data?: any;
  error?: string;
  actionId: string;
}

export interface AIResponse {
  message: string;
  type: 'text' | 'action_preview' | 'clarification' | 'result' | 'error';
  metadata?: ChatMessage['metadata'];
}

export interface ActionPreview {
  actionId: string;
  intentType: IntentType;
  title: string;
  description: string;
  fields: { label: string; value: string }[];
  requiresConfirmation: boolean;
  destructive?: boolean;
}

export interface CommandPaletteItem {
  label: string;
  description: string;
  icon: string;
  action: IntentType;
  shortcut?: string;
  category: 'project' | 'task' | 'user' | 'report' | 'system';
}