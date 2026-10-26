'use client';

import { cn } from '@/lib/utils';
import { motion } from 'framer-motion';
import { FolderKanban, CheckSquare, Users, Building, BarChart, FileText, Settings, ChevronRight, X } from 'lucide-react';
import { ConversationContext, EntityRef } from '@/types/chat';

interface ContextPanelProps {
  context: ConversationContext;
  onClose: () => void;
}

const ENTITY_ICONS = {
  project: FolderKanban,
  task: CheckSquare,
  user: Users,
  department: Building,
};

const ENTITY_COLORS = {
  project: 'text-blue-600 bg-blue-100 dark:bg-blue-900/30',
  task: 'text-green-600 bg-green-100 dark:bg-green-900/30',
  user: 'text-purple-600 bg-purple-100 dark:bg-purple-900/30',
  department: 'text-orange-600 bg-orange-100 dark:bg-orange-900/30',
};

export function ContextPanel({ context, onClose }: ContextPanelProps) {
  return (
    <motion.div
      initial={{ opacity: 0, x: -20 }}
      animate={{ opacity: 1, x: 0 }}
      exit={{ opacity: 0, x: -20 }}
      className="flex flex-col h-full bg-card"
    >
      {/* Header */}
      <div className="flex items-center justify-between p-4 border-b border-border">
        <h2 className="font-semibold">Context</h2>
        <button
          onClick={onClose}
          className="p-1 hover:bg-accent rounded-lg transition-colors"
          aria-label="Close context panel"
        >
          <X className="h-5 w-5 text-muted-foreground" />
        </button>
      </div>

      <div className="flex-1 overflow-y-auto p-4 space-y-6">
        {/* Active Entities */}
        <div className="space-y-3">
          <h3 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
            Active Entities
          </h3>
          
          {context.activeProject && (
            <EntityCard
              entity={context.activeProject}
              icon={FolderKanban}
              colorClass={ENTITY_COLORS.project}
            />
          )}
          
          {context.activeTask && (
            <EntityCard
              entity={context.activeTask}
              icon={CheckSquare}
              colorClass={ENTITY_COLORS.task}
            />
          )}
          
          {context.activeUser && (
            <EntityCard
              entity={context.activeUser}
              icon={Users}
              colorClass={ENTITY_COLORS.user}
            />
          )}
          
          {context.activeDepartment && (
            <EntityCard
              entity={context.activeDepartment}
              icon={Building}
              colorClass={ENTITY_COLORS.department}
            />
          )}
          
          {!context.activeProject && !context.activeTask && !context.activeUser && !context.activeDepartment && (
            <div className="text-center py-8 text-muted-foreground">
              <p className="text-sm">No active entity selected</p>
              <p className="text-xs mt-1">Start a conversation to set context</p>
            </div>
          )}
        </div>

        {/* Quick Actions */}
        <div className="space-y-3 border-t border-border pt-6">
          <h3 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
            Quick Actions
          </h3>
          <div className="grid grid-cols-2 gap-2">
            <QuickActionBtn
              icon={BarChart}
              label="Dept Progress"
              action="get_department_progress"
              description="View department report"
            />
            <QuickActionBtn
              icon={FileText}
              label="Project Summary"
              action="get_project_summary"
              description="View project summary"
            />
            <QuickActionBtn
              icon={FolderKanban}
              label="New Project"
              action="create_project"
              description="Create new project"
            />
            <QuickActionBtn
              icon={CheckSquare}
              label="New Task"
              action="create_task"
              description="Create new task"
            />
          </div>
        </div>

        {/* User Info */}
        <div className="border-t border-border pt-6">
          <h3 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-3">
            Your Profile
          </h3>
          <div className="flex items-center gap-3 p-3 bg-muted/50 rounded-lg">
            <div className="w-10 h-10 rounded-full bg-primary/10 flex items-center justify-center text-primary">
              <span className="font-medium">
                {context.userId.slice(0, 2).toUpperCase()}
              </span>
            </div>
            <div className="flex-1 min-w-0">
              <p className="font-medium truncate">{context.userId}</p>
              <p className="text-xs text-muted-foreground truncate">{context.userDepartment}</p>
            </div>
            <span className={cn(
              'px-2 py-0.5 text-xs font-medium rounded-full',
              context.userRole === 'Admin' && 'bg-purple-100 text-purple-700',
              context.userRole === 'Manager' && 'bg-blue-100 text-blue-700',
              context.userRole === 'Contributor' && 'bg-green-100 text-green-700',
              context.userRole === 'Viewer' && 'bg-gray-100 text-gray-700'
            )}>
              {context.userRole}
            </span>
          </div>
        </div>
      </div>
    </motion.div>
  );
}

function EntityCard({ entity, icon: Icon, colorClass }: { entity: EntityRef; icon: any; colorClass: string }) {
  return (
    <motion.div
      initial={{ opacity: 0, x: -10 }}
      animate={{ opacity: 1, x: 0 }}
      className="flex items-center gap-3 p-3 bg-muted/50 rounded-lg"
    >
      <div className={cn('p-2 rounded-lg', colorClass)}>
        <Icon className="h-5 w-5" />
      </div>
      <div className="flex-1 min-w-0">
        <p className="font-medium truncate">{entity.name}</p>
        <p className="text-xs text-muted-foreground capitalize">{entity.type}</p>
      </div>
    </motion.div>
  );
}

function QuickActionBtn({ icon: Icon, label, action, description }: { icon: any; label: string; action: string; description: string }) {
  return (
    <button
      className="flex flex-col items-center gap-1.5 p-3 text-center bg-muted/50 hover:bg-muted rounded-lg transition-colors border border-border"
      title={description}
    >
      <Icon className="h-5 w-5 text-muted-foreground" />
      <span className="text-xs font-medium">{label}</span>
    </button>
  );
}

// Need to add cn import
import { cn } from '@/lib/utils';