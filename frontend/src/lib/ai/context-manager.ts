import { ConversationContext, ChatMessage, EntityRef, Intent, UserRole, PendingAction } from '@/types/chat';
import { ActionPreview } from '@/lib/store/chat-store';

/**
 * Context Manager - Manages conversation state, entity references, and context
 */

export class ContextManager {
  private context: ConversationContext;
  private maxHistoryLength = 50;

  constructor(initialContext?: Partial<ConversationContext>) {
    this.context = {
      history: [],
      userRole: 'Viewer',
      userDepartment: '',
      userId: '',
      ...initialContext,
    };
  }

  /**
   * Get current context
   */
  getContext(): ConversationContext {
    return { ...this.context };
  }

  /**
   * Update context with partial updates
   */
  updateContext(updates: Partial<ConversationContext>): void {
    this.context = { ...this.context, ...updates };
  }

  /**
   * Add message to history
   */
  addMessage(message: ChatMessage): void {
    this.context.history = [...this.context.history, message].slice(-this.maxHistoryLength);
  }

  /**
   * Set active project focus
   */
  setActiveProject(project: EntityRef | undefined): void {
    this.context.activeProject = project;
  }

  /**
   * Set active task focus
   */
  setActiveTask(task: EntityRef | undefined): void {
    this.context.activeTask = task;
  }

  /**
   * Set active user focus
   */
  setActiveUser(user: EntityRef | undefined): void {
    this.context.activeUser = user;
  }

  /**
   * Set active department focus
   */
  setActiveDepartment(department: EntityRef | undefined): void {
    this.context.activeDepartment = department;
  }

  /**
   * Clear all active entities
   */
  clearActiveEntities(): void {
    this.context.activeProject = undefined;
    this.context.activeTask = undefined;
    this.context.activeUser = undefined;
    this.context.activeDepartment = undefined;
  }

  /**
   * Get active entity of specific type
   */
  getActiveEntity<T extends EntityRef>(type: 'project' | 'task' | 'user' | 'department'): T | undefined {
    switch (type) {
      case 'project':
        return this.context.activeProject as T | undefined;
      case 'task':
        return this.context.activeTask as T | undefined;
      case 'user':
        return this.context.activeUser as T | undefined;
      case 'department':
        return this.context.activeDepartment as T | undefined;
      default:
        return undefined;
    }
  }

  /**
   * Update user context (called after login)
   */
  setUserContext(userId: string, role: UserRole, department: string): void {
    this.context.userId = userId;
    this.context.userRole = role;
    this.context.userDepartment = department;
  }

  /**
   * Get user permissions for action
   */
  canPerformAction(actionType: string): boolean {
    const { userRole, userDepartment } = this.context;
    
    // Admin can do everything
    if (userRole === 'Admin') return true;
    
    // Department-scoped actions
    const departmentScopedActions = [
      'create_project', 'update_project', 'delete_project', 'list_projects',
      'create_task', 'update_task', 'delete_task', 'list_tasks',
      'assign_task', 'complete_task', 'reopen_task', 'block_task',
      'create_user', 'update_user', 'deactivate_user', 'list_users',
      'get_department_progress', 'export_report',
    ];
    
    if (departmentScopedActions.some(a => actionType.includes(a))) {
      return ['Admin', 'Manager', 'Contributor'].includes(userRole);
    }
    
    // Viewer actions
    const viewerActions = [
      'get_project', 'get_task', 'get_user', 'list_departments',
      'get_department_progress', 'get_project_summary', 'export_report',
    ];
    
    if (viewerActions.some(a => actionType.includes(a))) {
      return true; // All roles can view
    }
    
    // Admin-only actions
    const adminActions = [
      'create_department', 'update_department', 'deactivate_department',
      'backup', 'restore', 'health_check',
    ];
    
    if (adminActions.some(a => actionType.includes(a))) {
      return userRole === 'Admin';
    }
    
    return false;
  }

  /**
   * Get context summary for AI prompt
   */
  getContextSummary(): string {
    const parts: string[] = [];
    
    if (this.context.activeProject) {
      parts.push(`Active Project: ${this.context.activeProject.name} (${this.context.activeProject.id})`);
    }
    if (this.context.activeTask) {
      parts.push(`Active Task: ${this.context.activeTask.name} (${this.context.activeTask.id})`);
    }
    if (this.context.activeUser) {
      parts.push(`Active User: ${this.context.activeUser.name} (${this.context.activeUser.id})`);
    }
    if (this.context.activeDepartment) {
      parts.push(`Active Department: ${this.context.activeDepartment.name} (${this.context.activeDepartment.id})`);
    }
    
    parts.push(`User Role: ${this.context.userRole}`);
    parts.push(`User Department: ${this.context.userDepartment}`);
    parts.push(`Recent Messages: ${this.context.history.length}`);
    
    return parts.join('\n');
  }

  /**
   * Get recent history for AI context
   */
  getRecentHistory(count = 10): ChatMessage[] {
    return this.context.history.slice(-count);
  }

  /**
   * Set pending action awaiting confirmation
   */
  setPendingAction(action: ActionPreview | null): void {
    // Stored in chat store, but we track here for context
  }

  /**
   * Get entity suggestions for autocomplete
   */
  getEntitySuggestions(type: 'project' | 'task' | 'user' | 'department', query: string): EntityRef[] {
    // This would query the API in real implementation
    // For now, return context-aware suggestions
    const suggestions: EntityRef[] = [];
    
    if (type === 'project' && this.context.activeProject) {
      suggestions.push(this.context.activeProject);
    }
    if (type === 'task' && this.context.activeTask) {
      suggestions.push(this.context.activeTask);
    }
    if (type === 'user' && this.context.activeUser) {
      suggestions.push(this.context.activeUser);
    }
    if (type === 'department' && this.context.activeDepartment) {
      suggestions.push(this.context.activeDepartment);
    }
    
    return suggestions.filter(e => e.name.toLowerCase().includes(query.toLowerCase()));
  }

  /**
   * Reset context (new conversation)
   */
  reset(): void {
    this.context.history = [];
    this.context.activeProject = undefined;
    this.context.activeTask = undefined;
    this.context.activeUser = undefined;
    this.context.activeDepartment = undefined;
    this.context.pendingAction = undefined;
  }

  /**
   * Export context for persistence
   */
  export(): Partial<ConversationContext> {
    return {
      history: this.context.history.slice(-20), // Keep last 20
      activeProject: this.context.activeProject,
      activeTask: this.context.activeTask,
      activeUser: this.context.activeUser,
      activeDepartment: this.context.activeDepartment,
      userRole: this.context.userRole,
      userDepartment: this.context.userDepartment,
      userId: this.context.userId,
    };
  }

  /**
   * Import context from persistence
   */
  import(data: Partial<ConversationContext>): void {
    if (data.history) this.context.history = data.history;
    if (data.activeProject) this.context.activeProject = data.activeProject;
    if (data.activeTask) this.context.activeTask = data.activeTask;
    if (data.activeUser) this.context.activeUser = data.activeUser;
    if (data.activeDepartment) this.context.activeDepartment = data.activeDepartment;
    if (data.userRole) this.context.userRole = data.userRole;
    if (data.userDepartment) this.context.userDepartment = data.userDepartment;
    if (data.userId) this.context.userId = data.userId;
  }
}

// Singleton instance
let contextManagerInstance: ContextManager | null = null;

export function getContextManager(initialContext?: Partial<ConversationContext>): ContextManager {
  if (!contextManagerInstance) {
    contextManagerInstance = new ContextManager(initialContext);
  }
  return contextManagerInstance;
}

export function resetContextManager(): void {
  contextManagerInstance = null;
}