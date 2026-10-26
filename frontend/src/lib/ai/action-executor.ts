import { apiClient } from '@/lib/api/client';
import { Intent, IntentType, ActionResult, ActionPreview, ChatMessage } from '@/types/chat';
import { getContextManager } from './context-manager';

/**
 * Action Executor - Executes intents via the backend API
 */

interface ExecutionResult {
  success: boolean;
  data?: any;
  error?: string;
  actionId: string;
  previewData?: any;
}

export class ActionExecutor {
  private contextManager = getContextManager();

  /**
   * Execute an intent via the backend API
   */
  async execute(intent: any, intentType: string, actionId: string): Promise<ExecutionResult> {
    try {
      // Resolve entity references first
      const resolvedEntities = await this.resolveEntities(intent.entities);
      
      let result: any;
      
      switch (intent.type) {
        // Project actions
        case 'create_project':
          result = await this.createProject(intent.entities);
          break;
        case 'update_project':
          result = await this.updateProject(intent.entities);
          break;
        case 'delete_project':
          result = await this.deleteProject(intent.entities);
          break;
        case 'get_project':
          result = await this.getProject(intent.entities);
          break;
        case 'list_projects':
          result = await this.listProjects(intent.entities);
          break;
        case 'complete_project':
          result = await this.updateProjectStatus(intent.entities, 'Completed');
          break;
        case 'reopen_project':
          result = await this.updateProjectStatus(intent.entities, 'Active');
          break;

        // Task actions
        case 'create_task':
          result = await this.createTask(intent.entities);
          break;
        case 'update_task':
          result = await this.updateTask(intent.entities);
          break;
        case 'delete_task':
          result = await this.deleteTask(intent.entities);
          break;
        case 'get_task':
          result = await this.getTask(intent.entities);
          break;
        case 'list_tasks':
          result = await this.listTasks(intent.entities);
          break;
        case 'assign_task':
          result = await this.assignTask(intent.entities);
          break;
        case 'complete_task':
          result = await this.updateTaskStatus(intent.entities, 'Done');
          break;
        case 'reopen_task':
          result = await this.updateTaskStatus(intent.entities, 'InProgress');
          break;
        case 'block_task':
          result = await this.updateTaskStatus(intent.entities, 'Blocked');
          break;

        // User actions
        case 'create_user':
          result = await this.createUser(intent.entities);
          break;
        case 'update_user':
          result = await this.updateUser(intent.entities);
          break;
        case 'deactivate_user':
          result = await this.deactivateUser(intent.entities);
          break;
        case 'get_user':
          result = await this.getUser(intent.entities);
          break;
        case 'list_users':
          result = await this.listUsers(intent.entities);
          break;

        // Department actions
        case 'create_department':
          result = await this.createDepartment(intent.entities);
          break;
        case 'update_department':
          result = await this.updateDepartment(intent.entities);
          break;
        case 'deactivate_department':
          result = await this.deactivateDepartment(intent.entities);
          break;
        case 'list_departments':
          result = await this.listDepartments(intent.entities);
          break;

        // Report actions
        case 'get_department_progress':
          result = await this.getDepartmentProgress(intent.entities);
          break;
        case 'get_project_summary':
          result = await this.getProjectSummary(intent.entities);
          break;
        case 'export_report':
          result = await this.exportReport(intent.entities);
          break;

        // System actions
        case 'health_check':
          result = await this.healthCheck();
          break;
        case 'backup':
          result = await this.createBackup();
          break;
        case 'restore':
          result = await this.restoreBackup(intent.entities);
          break;

        default:
          throw new Error(`Unknown intent type: ${intent.type}`);
      }

      // Update context with result
      this.updateContextFromResult(intent.type, result);

      return {
        success: true,
        data: result,
        actionId,
        previewData: this.generatePreviewData(intent.type, intent.entities, result),
      };
    } catch (error: any) {
      return {
        success: false,
        error: error.response?.data?.detail || error.message || 'Unknown error',
        actionId,
      };
    }
  }

  /**
   * Resolve entity names to IDs using context or API search
   */
  private async resolveEntities(entities: Record<string, any>): Promise<Record<string, any>> {
    const resolved = { ...entities };
    const context = this.contextManager.getContext();

    // Resolve department
    if (entities.departmentName && !entities.departmentId) {
      if (context.activeDepartment?.name === entities.departmentName) {
        resolved.departmentId = context.activeDepartment.id;
      } else if (context.userDepartment) {
        // Default to user's department
        // In real implementation, search via API
      }
    }

    // Resolve project
    if (entities.projectName && !entities.projectId) {
      if (context.activeProject?.name === entities.projectName) {
        resolved.projectId = context.activeProject.id;
      }
    }

    // Resolve assignee
    if (entities.assignee && !entities.assigneeId) {
      if (context.activeUser?.name === entities.assignee) {
        resolved.assigneeId = context.activeUser.id;
      }
    }

    return resolved;
  }

  /**
   * Generate preview data for action confirmation
   */
  private generatePreviewData(actionType: string, entities: Record<string, any>, result: any): any {
    const preview: any = {
      actionType,
      entities: { ...entities },
    };

    if (result) {
      preview.result = {
        id: result.id,
        name: result.name || result.title,
        status: result.status,
      };
    }

    return preview;
  }

  /**
   * Update context based on action result
   */
  private updateContextFromResult(actionType: string, result: any): void {
    const context = getContextManager();

    if (result?.id && result?.name) {
      if (actionType.startsWith('create_') || actionType.startsWith('get_') || actionType.startsWith('update_')) {
        if (actionType.includes('project')) {
          context.setActiveProject({ id: result.id, name: result.name, type: 'project' });
        } else if (actionType.includes('task')) {
          context.setActiveTask({ id: result.id, name: result.title || result.name, type: 'task' });
        } else if (actionType.includes('user')) {
          context.setActiveUser({ id: result.id, name: result.displayName || result.name, type: 'user' });
        } else if (actionType.includes('department')) {
          context.setActiveDepartment({ id: result.id, name: result.name, type: 'department' });
        }
      }
    }
  }

  // ==================== API Methods ====================

  // Projects
  private async createProject(entities: any) {
    const data = {
      departmentId: entities.departmentId || entities.departmentId,
      ownerUserId: entities.ownerUserId, // Would be current user
      name: entities.name || entities.projectName,
      description: entities.description || '',
      status: entities.status || 'Planned',
      startAt: entities.startAt,
      dueAt: entities.dueAt || entities.dueDate,
    };
    return apiClient.createProject(data);
  }

  private async updateProject(entities: any) {
    const data = {
      name: entities.name,
      description: entities.description,
      status: entities.status,
      startAt: entities.startAt,
      dueAt: entities.dueAt,
    };
    // Need version for ETag - would come from context
    return apiClient.updateProject(entities.id || entities.projectId, data, 1);
  }

  private async deleteProject(entities: any) {
    return apiClient.deleteProject(entities.id || entities.projectId, 1);
  }

  private async getProject(entities: any) {
    return apiClient.getProject(entities.id || entities.projectId);
  }

  private async listProjects(entities: any) {
    return apiClient.getProjects({
      departmentId: entities.departmentId,
      status: entities.status,
      ownerUserId: entities.ownerUserId,
    });
  }

  private async updateProjectStatus(entities: any, status: string) {
    return apiClient.updateProjectStatus(entities.id || entities.projectId, status, 1);
  }

  // Tasks
  private async createTask(entities: any) {
    const data = {
      title: entities.title || entities.taskName,
      description: entities.description || '',
      priority: entities.priority || 'Medium',
      dueAt: entities.dueAt || entities.dueDate,
      assignedToUserId: entities.assigneeId || entities.assignee,
    };
    return apiClient.createTask(entities.projectId || entities.projectId, data);
  }

  private async updateTask(entities: any) {
    const data = {
      title: entities.title,
      description: entities.description,
      priority: entities.priority,
      dueAt: entities.dueAt,
      assignedToUserId: entities.assigneeId,
    };
    return apiClient.updateTask(entities.id || entities.taskId, data, 1);
  }

  private async deleteTask(entities: any) {
    return apiClient.deleteTask(entities.id || entities.taskId, 1);
  }

  private async getTask(entities: any) {
    return apiClient.getTask(entities.id || entities.taskId);
  }

  private async listTasks(entities: any) {
    return apiClient.getProjectTasks(entities.projectId);
  }

  private async assignTask(entities: any) {
    const task = await apiClient.getTask(entities.id || entities.taskId);
    return apiClient.updateTask(entities.id || entities.taskId, {
      ...task,
      assignedToUserId: entities.assigneeId || entities.assignee,
    }, 1);
  }

  private async updateTaskStatus(entities: any, status: string) {
    return apiClient.updateTaskStatus(entities.id || entities.taskId, status, 1);
  }

  // Users
  private async createUser(entities: any) {
    const data = {
      departmentId: entities.departmentId,
      email: entities.email,
      displayName: entities.displayName,
      password: entities.password || 'TempPass123!',
      role: entities.role || 'Contributor',
    };
    return apiClient.createUser(data);
  }

  private async updateUser(entities: any) {
    const data = {
      email: entities.email,
      displayName: entities.displayName,
      role: entities.role,
    };
    return apiClient.updateUser(entities.id || entities.userId, data);
  }

  private async deactivateUser(entities: any) {
    return apiClient.deactivateUser(entities.id || entities.userId);
  }

  private async getUser(entities: any) {
    // Would need a get user by ID endpoint - using list for now
    const users = await apiClient.getUsers();
    return users.find((u: any) => u.id === entities.id || u.email === entities.email);
  }

  private async listUsers(entities: any) {
    if (entities.departmentId) {
      return apiClient.getUsersByDepartment(entities.departmentId);
    }
    return apiClient.getUsers();
  }

  // Departments
  private async createDepartment(entities: any) {
    return apiClient.createDepartment(entities.name || entities.departmentName);
  }

  private async updateDepartment(entities: any) {
    return apiClient.updateDepartment(entities.id || entities.departmentId, entities.name);
  }

  private async deactivateDepartment(entities: any) {
    return apiClient.deactivateDepartment(entities.id || entities.departmentId);
  }

  private async listDepartments() {
    return apiClient.getDepartments();
  }

  // Reports
  private async getDepartmentProgress(entities: any) {
    return apiClient.getDepartmentProgress(entities.departmentId);
  }

  private async getProjectSummary(entities: any) {
    return apiClient.getProjectSummary(entities.id || entities.projectId);
  }

  private async exportReport(entities: any) {
    const format = entities.format || 'json';
    return apiClient.exportReport(format, entities.departmentId);
  }

  // System
  private async healthCheck() {
    return apiClient.checkHealth();
  }

  private async createBackup() {
    // Would call backup endpoint if available
    throw new Error('Backup endpoint not implemented in frontend');
  }

  private async restoreBackup(entities: any) {
    throw new Error('Restore endpoint not implemented in frontend');
  }
}

// Singleton
let executorInstance: ActionExecutor | null = null;

export function getActionExecutor(): ActionExecutor {
  if (!executorInstance) {
    executorInstance = new ActionExecutor();
  }
  return executorInstance;
}