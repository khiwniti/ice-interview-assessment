import { Intent, IntentType, ConversationContext, EntityRef } from '@/types/chat';

/**
 * Intent Parser - Parses natural language into structured intents
 * Uses pattern matching and keyword extraction (no external NLP service required)
 * Can be extended with OpenAI/Anthropic for more sophisticated parsing
 */

// Action patterns with keywords and required parameters
const ACTION_PATTERNS: Array<{
  type: IntentType;
  keywords: string[];
  requiredParams: string[];
  destructive?: boolean;
  description: string;
}> = [
  // Project actions
  { type: 'create_project', keywords: ['create', 'new', 'make', 'add'], requiredParams: ['name'], destructive: false, description: 'Create a new project' },
  { type: 'update_project', keywords: ['update', 'edit', 'modify', 'change'], requiredParams: ['id'], destructive: false, description: 'Update an existing project' },
  { type: 'delete_project', keywords: ['delete', 'remove', 'destroy'], requiredParams: ['id'], destructive: true, description: 'Delete a project' },
  { type: 'get_project', keywords: ['show', 'view', 'get', 'see'], requiredParams: ['id'], destructive: false, description: 'View project details' },
  { type: 'list_projects', keywords: ['list', 'show all', 'all projects'], requiredParams: [], destructive: false, description: 'List all projects' },
  { type: 'complete_project', keywords: ['complete', 'finish', 'done'], requiredParams: ['id'], destructive: false, description: 'Mark project as completed' },
  { type: 'reopen_project', keywords: ['reopen', 'reactivate'], requiredParams: ['id'], destructive: false, description: 'Reopen a completed project' },

  // Task actions
  { type: 'create_task', keywords: ['create task', 'new task', 'add task'], requiredParams: ['title', 'projectId'], destructive: false, description: 'Create a new task' },
  { type: 'update_task', keywords: ['update task', 'edit task', 'modify task'], requiredParams: ['id'], destructive: false, description: 'Update a task' },
  { type: 'delete_task', keywords: ['delete task', 'remove task'], requiredParams: ['id'], destructive: true, description: 'Delete a task' },
  { type: 'get_task', keywords: ['show task', 'view task', 'get task'], requiredParams: ['id'], destructive: false, description: 'View task details' },
  { type: 'list_tasks', keywords: ['list tasks', 'show tasks', 'all tasks'], requiredParams: ['projectId'], destructive: false, description: 'List tasks for a project' },
  { type: 'assign_task', keywords: ['assign', 'give to', 'allocate'], requiredParams: ['id', 'assignee'], destructive: false, description: 'Assign task to user' },
  { type: 'complete_task', keywords: ['complete task', 'finish task', 'done task'], requiredParams: ['id'], destructive: false, description: 'Mark task as done' },
  { type: 'reopen_task', keywords: ['reopen task', 'reactivate task'], requiredParams: ['id'], destructive: false, description: 'Reopen a completed task' },
  { type: 'block_task', keywords: ['block task', 'blocked'], requiredParams: ['id'], destructive: false, description: 'Block a task' },

  // User actions
  { type: 'create_user', keywords: ['create user', 'new user', 'add user'], requiredParams: ['email', 'displayName', 'departmentId'], destructive: false, description: 'Create a new user' },
  { type: 'update_user', keywords: ['update user', 'edit user'], requiredParams: ['id'], destructive: false, description: 'Update a user' },
  { type: 'deactivate_user', keywords: ['deactivate user', 'disable user'], requiredParams: ['id'], destructive: true, description: 'Deactivate a user' },
  { type: 'get_user', keywords: ['show user', 'view user', 'get user'], requiredParams: ['id'], destructive: false, description: 'View user details' },
  { type: 'list_users', keywords: ['list users', 'show users', 'all users'], requiredParams: [], destructive: false, description: 'List all users' },

  // Department actions
  { type: 'create_department', keywords: ['create department', 'new department', 'add department'], requiredParams: ['name'], destructive: false, description: 'Create a new department' },
  { type: 'update_department', keywords: ['update department', 'edit department'], requiredParams: ['id'], destructive: false, description: 'Update a department' },
  { type: 'deactivate_department', keywords: ['deactivate department', 'disable department'], requiredParams: ['id'], destructive: true, description: 'Deactivate a department' },
  { type: 'list_departments', keywords: ['list departments', 'show departments', 'all departments'], requiredParams: [], destructive: false, description: 'List all departments' },

  // Report actions
  { type: 'get_department_progress', keywords: ['progress', 'report', 'department progress', 'progress report'], requiredParams: [], destructive: false, description: 'View department progress report' },
  { type: 'get_project_summary', keywords: ['project summary', 'project report', 'summary'], requiredParams: ['id'], destructive: false, description: 'View project summary report' },
  { type: 'export_report', keywords: ['export', 'download', 'csv', 'json'], requiredParams: [], destructive: false, description: 'Export report as JSON or CSV' },

  // System actions
  { type: 'health_check', keywords: ['health', 'status', 'health check'], requiredParams: [], destructive: false, description: 'Check system health' },
  { type: 'backup', keywords: ['backup', 'backup database'], requiredParams: [], destructive: false, description: 'Create database backup' },
  { type: 'restore', keywords: ['restore', 'restore database'], requiredParams: [], destructive: true, description: 'Restore database from backup' },
];

// Entity reference patterns
const ENTITY_PATTERNS = {
  project: [
    /project\s+["']?([^"']+)["']?/i,
    /["']([^"']+)["']\s+project/i,
    /#(\d+)/, // Project ID reference
  ],
  task: [
    /task\s+["']?([^"']+)["']?/i,
    /["']([^"']+)["']\s+task/i,
    /#(\d+)/,
  ],
  user: [
    /@(\w+)/i, // @username
    /user\s+["']?([^"']+)["']?/i,
    /assign\s+to\s+(\w+)/i,
  ],
  department: [
    /department\s+["']?([^"']+)["']?/i,
    /in\s+(\w+)\s+department/i,
    /for\s+(\w+)\s+department/i,
  ],
  date: [
    /due\s+(\w+\s+\d+(?:st|nd|rd|th)?)/i,
    /by\s+(\w+\s+\d+(?:st|nd|rd|th)?)/i,
    /(\d{4}-\d{2}-\d{2})/,
  ],
  priority: [
    /priority\s+(high|medium|low|critical)/i,
    /(high|medium|low|critical)\s+priority/i,
  ],
  status: [
    /status\s+(planned|active|onhold|completed|cancelled|todo|inprogress|blocked|done)/i,
    /mark\s+(as\s+)?(done|completed|active|blocked)/i,
  ],
};

export class IntentParser {
  private context: ConversationContext;

  constructor(context: ConversationContext) {
    this.context = context;
  }

  /**
   * Parse natural language message into structured intent
   */
  async parse(message: string): Promise<Intent> {
    const lowerMessage = message.toLowerCase().trim();
    
    // Find matching action pattern
    const matchedAction = this.findMatchingAction(lowerMessage);
    
    if (!matchedAction) {
      return this.createUnknownIntent(message);
    }

    // Extract entities from message
    const entities = this.extractEntities(lowerMessage, matchedAction.type);
    
    // Check for missing required parameters
    const missingFields = this.checkMissingFields(matchedAction, entities);
    
    // Determine confidence based on keyword matches
    const confidence = this.calculateConfidence(lowerMessage, matchedAction);
    
    return {
      type: matchedAction.type,
      entities,
      confidence,
      requiresConfirmation: matchedAction.destructive || missingFields.length === 0,
      missingFields,
      originalMessage: message,
    };
  }

  /**
   * Find the best matching action pattern
   */
  private findMatchingAction(message: string): typeof ACTION_PATTERNS[0] | null {
    let bestMatch: typeof ACTION_PATTERNS[0] | null = null;
    let bestScore = 0;

    for (const action of ACTION_PATTERNS) {
      let score = 0;
      for (const keyword of action.keywords) {
        if (message.includes(keyword.toLowerCase())) {
          score += keyword.length; // Longer keywords = higher score
        }
      }
      if (score > bestScore) {
        bestScore = score;
        bestMatch = action;
      }
    }

    return bestMatch;
  }

  /**
   * Extract entities from message based on action type
   */
  private extractEntities(message: string, actionType: IntentType): Record<string, any> {
    const entities: Record<string, any> = {};

    // Extract project references
    for (const pattern of ENTITY_PATTERNS.project) {
      const match = message.match(pattern);
      if (match) {
        entities.projectName = match[1]?.trim();
        break;
      }
    }

    // Extract task references
    for (const pattern of ENTITY_PATTERNS.task) {
      const match = message.match(pattern);
      if (match) {
        entities.taskName = match[1]?.trim();
        break;
      }
    }

    // Extract user references (@username)
    for (const pattern of ENTITY_PATTERNS.user) {
      const match = message.match(pattern);
      if (match) {
        entities.assignee = match[1]?.trim();
        break;
      }
    }

    // Extract department references
    for (const pattern of ENTITY_PATTERNS.department) {
      const match = message.match(pattern);
      if (match) {
        entities.departmentName = match[1]?.trim();
        break;
      }
    }

    // Extract dates
    for (const pattern of ENTITY_PATTERNS.date) {
      const match = message.match(pattern);
      if (match) {
        entities.dueDate = this.parseDate(match[1]);
        break;
      }
    }

    // Extract priority
    for (const pattern of ENTITY_PATTERNS.priority) {
      const match = message.match(pattern);
      if (match) {
        entities.priority = match[1]?.toLowerCase();
        break;
      }
    }

    // Extract status
    for (const pattern of ENTITY_PATTERNS.status) {
      const match = message.match(pattern);
      if (match) {
        entities.status = this.normalizeStatus(match[1] || match[2]);
        break;
      }
    }

    // Use context for defaults
    if (!entities.departmentName && this.context.activeDepartment) {
      entities.departmentId = this.context.activeDepartment.id;
    }
    if (!entities.projectId && this.context.activeProject) {
      entities.projectId = this.context.activeProject.id;
    }

    return entities;
  }

  /**
   * Check for missing required fields
   */
  private checkMissingFields(action: typeof ACTION_PATTERNS[0], entities: Record<string, any>): string[] {
    const missing: string[] = [];
    
    for (const required of action.requiredParams) {
      // Check various possible keys for the same entity
      const keys = this.getPossibleKeys(required);
      const hasValue = keys.some(key => entities[key] !== undefined && entities[key] !== '');
      if (!hasValue) {
        missing.push(required);
      }
    }
    
    return missing;
  }

  private getPossibleKeys(param: string): string[] {
    const keyMap: Record<string, string[]> = {
      'name': ['name', 'projectName', 'taskName', 'departmentName', 'displayName'],
      'id': ['id', 'projectId', 'taskId', 'userId', 'departmentId'],
      'title': ['title', 'taskName'],
      'projectId': ['projectId', 'project'],
      'assignee': ['assignee', 'assignedTo', 'userId'],
      'departmentId': ['departmentId', 'department'],
      'email': ['email'],
      'displayName': ['displayName', 'name'],
      'dueDate': ['dueDate', 'due'],
      'priority': ['priority'],
      'status': ['status'],
    };
    return keyMap[param] || [param];
  }

  private calculateConfidence(message: string, action: typeof ACTION_PATTERNS[0]): number {
    let score = 0;
    for (const keyword of action.keywords) {
      if (message.includes(keyword.toLowerCase())) {
        score += 1;
      }
    }
    // Normalize to 0-1 range (max possible keywords per action is ~4)
    return Math.min(score / 4, 1);
  }

  private createUnknownIntent(message: string): Intent {
    return {
      type: 'list_projects' as IntentType, // Default fallback
      entities: {},
      confidence: 0.1,
      requiresConfirmation: false,
      missingFields: ['intent'],
      originalMessage: message,
    };
  }

  private parseDate(dateStr: string): string {
    // Simple date parsing - in production, use date-fns or similar
    const now = new Date();
    const lower = dateStr.toLowerCase();
    
    if (lower.includes('today')) return now.toISOString().split('T')[0];
    if (lower.includes('tomorrow')) {
      const tomorrow = new Date(now);
      tomorrow.setDate(tomorrow.getDate() + 1);
      return tomorrow.toISOString().split('T')[0];
    }
    if (lower.includes('next week')) {
      const nextWeek = new Date(now);
      nextWeek.setDate(nextWeek.getDate() + 7);
      return nextWeek.toISOString().split('T')[0];
    }
    if (lower.includes('next month')) {
      const nextMonth = new Date(now);
      nextMonth.setMonth(nextMonth.getMonth() + 1);
      return nextMonth.toISOString().split('T')[0];
    }
    
    // Try to parse as ISO date
    const parsed = new Date(dateStr);
    if (!isNaN(parsed.getTime())) {
      return parsed.toISOString().split('T')[0];
    }
    
    return now.toISOString().split('T')[0];
  }

  private normalizeStatus(status: string): string {
    const statusMap: Record<string, string> = {
      'done': 'Done',
      'completed': 'Completed',
      'active': 'Active',
      'planned': 'Planned',
      'onhold': 'OnHold',
      'on hold': 'OnHold',
      'cancelled': 'Cancelled',
      'todo': 'Todo',
      'inprogress': 'InProgress',
      'in progress': 'InProgress',
      'blocked': 'Blocked',
    };
    return statusMap[status.toLowerCase()] || status;
  }

  /**
   * Resolve entity references from context or search
   */
  async resolveEntities(entities: Record<string, any>): Promise<Record<string, any>> {
    const resolved = { ...entities };
    
    // Resolve project by name
    if (entities.projectName && !entities.projectId) {
      // Would search via API - placeholder
      resolved.projectId = `resolved-${entities.projectName}`;
    }
    
    // Resolve user by username
    if (entities.assignee && !entities.assigneeId) {
      resolved.assigneeId = `resolved-${entities.assignee}`;
    }
    
    // Resolve department by name
    if (entities.departmentName && !entities.departmentId) {
      resolved.departmentId = `resolved-${entities.departmentName}`;
    }
    
    return resolved;
  }
}