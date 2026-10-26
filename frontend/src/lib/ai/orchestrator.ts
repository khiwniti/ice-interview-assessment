import { IntentParser } from './intent-parser';
import { ActionExecutor } from './action-executor';
import { getContextManager } from './context-manager';
import { Intent, AIResponse, ChatMessage, ConversationContext } from '@/types/chat';

/**
 * AI Orchestrator - Coordinates the full AI chat flow
 */

interface OrchestratorConfig {
  systemPrompt: string;
  maxRetries: number;
}

const DEFAULT_SYSTEM_PROMPT = `You are an AI assistant for the Business Analytics platform.
You help users manage projects, tasks, users, departments, and reports through natural language.

AVAILABLE ACTIONS:
- create_project: Create a new project (name, department, owner, status, dates)
- update_project: Update project details
- delete_project: Delete a project (requires confirmation)
- get_project: View project details
- list_projects: List all projects
- complete_project: Mark project as completed
- reopen_project: Reopen a completed project
- create_task: Create a new task (title, project, priority, due date, assignee)
- update_task: Update task details
- delete_task: Delete a task (requires confirmation)
- get_task: View task details
- list_tasks: List tasks for a project
- assign_task: Assign task to user
- complete_task: Mark task as done
- reopen_task: Reopen a completed task
- block_task: Block a task
- create_user: Create a new user (email, name, department, role)
- update_user: Update user details
- deactivate_user: Deactivate a user (requires confirmation)
- get_user: View user details
- list_users: List all users
- create_department: Create a new department
- update_department: Update department
- deactivate_department: Deactivate department (requires confirmation)
- list_departments: List all departments
- get_department_progress: View department progress report
- get_project_summary: View project summary report
- export_report: Export report as JSON or CSV
- health_check: Check system health
- backup: Create database backup
- restore: Restore database from backup

RULES:
1. ALWAYS confirm before destructive actions (delete, deactivate)
2. Ask for missing required parameters
3. Respect department scoping - users can only access their department
4. Use department context for defaults
4. Reference existing entities by name when possible
5. Never hallucinate - if unsure, ask for clarification

RESPONSE FORMAT:
- If action needed: Return action preview for user confirmation
- If clarification needed: Ask specific questions
- If info request: Provide data with context
- If error: Explain clearly with recovery steps`;

export class AIOrchestrator {
  private intentParser: IntentParser;
  private actionExecutor: ActionExecutor;
  private contextManager = getContextManager();
  private config: OrchestratorConfig;

  constructor(config?: Partial<OrchestratorConfig>) {
    const context = this.contextManager.getContext();
    this.intentParser = new IntentParser(context);
    this.actionExecutor = new ActionExecutor();
    this.config = {
      systemPrompt: config?.systemPrompt || DEFAULT_SYSTEM_PROMPT,
      maxRetries: config?.maxRetries ?? 3,
    };
  }

  /**
   * Process a user message through the full AI pipeline
   */
  async processMessage(message: string): Promise<AIResponse> {
    // 1. Update context with user message
    this.contextManager.addMessage({
      id: crypto.randomUUID(),
      role: 'user',
      content: message,
      timestamp: new Date(),
    });

    // 2. Parse intent
    const intent = await this.intentParser.parse(message);

    // 3. Handle low confidence
    if (intent.confidence < 0.3) {
      return this.createClarificationResponse(
        "I'm not sure what you'd like to do. Could you rephrase or try one of these commands?",
        ['Create a project', 'Show department progress', 'Create a task', 'Export report']
      );
    }

    // 4. Handle missing fields
    if (intent.missingFields.length > 0) {
      return this.createClarificationResponse(
        `I need a few more details to ${this.getActionDescription(intent.type)}:`,
        intent.missingFields.map(f => ({ key: f, label: this.formatFieldLabel(f), required: true }))
      );
    }

    // 5. Check permissions
    if (!this.contextManager.canPerformAction(intent.type)) {
      return {
        message: "You don't have permission to perform this action.",
        type: 'error',
      };
    }

    // 6. Handle destructive actions requiring confirmation
    if (intent.requiresConfirmation && !this.isConfirmed(intent)) {
      const preview = await this.generateActionPreview(intent);
      return {
        message: `Please confirm this action:`,
        type: 'action_preview',
        metadata: { previewData: preview, intentType: intent.type },
      };
    }

    // 7. Execute action
    const actionId = crypto.randomUUID();
    const result = await this.actionExecutor.execute(intent, intent.type, actionId);

    // 8. Generate response
    if (result.success) {
      return this.createSuccessResponse(intent.type, result);
    } else {
      return this.createErrorResponse(result.error || 'Action failed');
    }
  }

  /**
   * Handle confirmed action execution
   */
  async executeConfirmedAction(actionId: string, intent: any): Promise<AIResponse> {
    const result = await this.actionExecutor.execute(intent, intent.type, actionId);
    
    if (result.success) {
      return this.createSuccessResponse(intent.type, result);
    } else {
      return this.createErrorResponse(result.error || 'Action failed');
    }
  }

  /**
   * Handle clarification response
   */
  async handleClarification(fields: Record<string, any>, originalIntent: any): Promise<AIResponse> {
    // Merge clarified fields with original intent
    const clarifiedIntent = {
      ...originalIntent,
      entities: { ...originalIntent.entities, ...fields },
      missingFields: [],
      requiresConfirmation: originalIntent.requiresConfirmation,
    };

    // Check if still missing fields
    const intentParser = new IntentParser(this.contextManager.getContext());
    const missingFields = intentParser.checkMissingFields(
      { requiredParams: originalIntent.missingFields } as any,
      clarifiedIntent.entities
    );

    if (missingFields.length > 0) {
      return this.createClarificationResponse(
        "Still need a few more details:",
        missingFields.map(f => ({ key: f, label: this.formatFieldLabel(f), required: true }))
      );
    }

    // Execute with clarified intent
    const actionId = crypto.randomUUID();
    const result = await this.actionExecutor.execute(clarifiedIntent, clarifiedIntent.type, actionId);

    if (result.success) {
      return this.createSuccessResponse(clarifiedIntent.type, result);
    } else {
      return this.createErrorResponse(result.error || 'Action failed');
    }
  }

  /**
   * Generate action preview for confirmation
   */
  private async generateActionPreview(intent: any): Promise<any> {
    const context = getContextManager().getContext();
    
    const preview: any = {
      actionId: crypto.randomUUID(),
      intentType: intent.type,
      title: this.getActionTitle(intent.type),
      description: this.getActionDescription(intent.type),
      fields: [],
      requiresConfirmation: intent.requiresConfirmation,
      destructive: this.isDestructive(intent.type),
    };

    // Add relevant fields to preview
    for (const [key, value] of Object.entries(intent.entities)) {
      if (value !== undefined && value !== '') {
        preview.fields.push({
          label: this.formatFieldLabel(key),
          value: typeof value === 'object' ? JSON.stringify(value) : String(value),
        });
      }
    }

    // Add context defaults
    const context = getContextManager().getContext();
    if (!intent.entities.departmentId && context.activeDepartment) {
      preview.fields.push({ label: 'Department', value: context.activeDepartment.name });
    }

    return preview;
  }

  /**
   * Create success response
   */
  private createSuccessResponse(intentType: string, result: any): AIResponse {
    const messages: Record<string, string> = {
      create_project: `✅ Project "${result.name}" created successfully!`,
      update_project: `✅ Project "${result.name}" updated successfully!`,
      delete_project: `✅ Project deleted successfully.`,
      complete_project: `✅ Project "${result.name}" marked as completed!`,
      reopen_project: `✅ Project "${result.name}" reopened!`,
      create_task: `✅ Task "${result.title}" created successfully!`,
      update_task: `✅ Task "${result.title}" updated!`,
      delete_task: `✅ Task deleted.`,
      complete_task: `✅ Task "${result.title}" marked as done!`,
      reopen_task: `✅ Task "${result.title}" reopened!`,
      assign_task: `✅ Task assigned to ${result.assignee?.displayName || 'user'}!`,
      create_user: `✅ User "${result.displayName}" created!`,
      update_user: `✅ User "${result.displayName}" updated!`,
      deactivate_user: `✅ User deactivated.`,
      create_department: `✅ Department "${result.name}" created!`,
      update_department: `✅ Department "${result.name}" updated!`,
      deactivate_department: `✅ Department deactivated.`,
      get_department_progress: `📊 Here's the department progress report:`,
      get_project_summary: `📋 Here's the project summary:`,
      export_report: `✅ Report exported successfully!`,
      health_check: `✅ System is healthy!`,
      backup: `✅ Backup created!`,
      restore: `✅ Database restored!`,
    };

    const message = messages[intentType] || `✅ Action completed successfully!`;
    
    return {
      message,
      type: intentType.includes('progress') || intentType.includes('summary') ? 'result' : 'text',
      metadata: { resultData: result, intentType },
    };
  }

  /**
   * Create error response
   */
  private createErrorResponse(error: string): AIResponse {
    return {
      message: `❌ Error: ${error}`,
      type: 'error',
      metadata: { errorData: { message: error } },
    };
  }

  /**
   * Create clarification response
   */
  private createClarificationResponse(message: string, fields: Array<{ key: string; label: string; required: boolean }>): AIResponse {
    return {
      message,
      type: 'clarification',
      metadata: {
        clarificationFields: fields.map(f => ({
          key: f.key,
          label: f.label,
          type: 'text',
          required: f.required,
        })),
      },
    };
  }

  // Helper methods
  private getActionTitle(type: string): string {
    const titles: Record<string, string> = {
      create_project: 'Create Project',
      update_project: 'Update Project',
      delete_project: 'Delete Project',
      complete_project: 'Complete Project',
      reopen_project: 'Reopen Project',
      create_task: 'Create Task',
      update_task: 'Update Task',
      delete_task: 'Delete Task',
      complete_task: 'Complete Task',
      reopen_task: 'Reopen Task',
      assign_task: 'Assign Task',
      block_task: 'Block Task',
      create_user: 'Create User',
      update_user: 'Update User',
      deactivate_user: 'Deactivate User',
      create_department: 'Create Department',
      update_department: 'Update Department',
      deactivate_department: 'Deactivate Department',
      get_department_progress: 'Department Progress Report',
      get_project_summary: 'Project Summary Report',
      export_report: 'Export Report',
      health_check: 'Health Check',
      backup: 'Backup Database',
      restore: 'Restore Database',
    };
    return titles[intentType] || 'Action';
  }

  private getActionDescription(type: string): string {
    const descriptions: Record<string, string> = {
      create_project: 'Create a new project in your department',
      update_project: 'Update project details',
      delete_project: 'Permanently delete a project (cannot be undone)',
      complete_project: 'Mark project as completed (requires all tasks done)',
      reopen_project: 'Reopen a completed project for editing',
      create_task: 'Create a new task in a project',
      update_task: 'Update task details',
      delete_task: 'Permanently delete a task',
      complete_task: 'Mark task as done',
      reopen_task: 'Reopen a completed task',
      assign_task: 'Assign task to a team member',
      block_task: 'Block a task',
      create_user: 'Create a new user account',
      update_user: 'Update user details',
      deactivate_user: 'Deactivate user account',
      create_department: 'Create a new department',
      update_department: 'Update department details',
      deactivate_department: 'Deactivate department',
      get_department_progress: 'View task-weighted progress report',
      get_project_summary: 'View project summary with task breakdown',
      export_report: 'Export report as JSON or CSV',
      health_check: 'Check system health status',
      backup: 'Create database backup',
      restore: 'Restore database from backup',
    };
    return descriptions[intentType] || 'Perform action';
  }

  private isDestructive(type: string): boolean {
    return ['delete_project', 'delete_task', 'deactivate_user', 'deactivate_department', 'restore'].includes(type);
  }

  private isConfirmed(intent: any): boolean {
    // Check if this action was already confirmed
    return intent.confirmed === true;
  }

  private formatFieldLabel(key: string): string {
    return key
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, str => str.toUpperCase())
      .replace(/Id$/, ' ID')
      .replace(/At$/, ' At')
      .replace(/([a-z])([A-Z])/g, '$1 $2');
  }
}

// Singleton
let orchestratorInstance: AIOrchestrator | null = null;

export function getAIOrchestrator(config?: any): AIOrchestrator {
  if (!orchestratorInstance) {
    orchestratorInstance = new AIOrchestrator(config);
  }
  return orchestratorInstance;
}

export function resetAIOrchestrator(): void {
  orchestratorInstance = null;
}