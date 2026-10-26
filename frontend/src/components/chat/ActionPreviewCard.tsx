'use client';

import { cn } from '@/lib/utils';
import { motion } from 'framer-motion';
import { AlertTriangle, Trash2, Shield, CheckCircle, XCircle, ChevronLeft, ChevronRight } from 'lucide-react';
import { ActionPreview, ChatMessage } from '@/types/chat';

interface ActionPreviewCardProps {
  action: ActionPreview;
  onConfirm: (intent: any) => void;
  onReject: () => void;
}

export function ActionPreviewCard({ action, onConfirm, onReject }: ActionPreviewCardProps) {
  const intent = action.intentType;

  return (
    <motion.div
      initial={{ opacity: 0, y: 20, scale: 0.95 }}
      animate={{ opacity: 1, y: 0, scale: 1 }}
      exit={{ opacity: 0, y: -20, scale: 0.95 }}
      className="flex flex-col gap-4 p-4 bg-card border border-border rounded-xl shadow-lg"
    >
      {/* Header */}
      <div className="flex items-center gap-3">
        {action.destructive ? (
          <div className="p-2 bg-destructive/10 rounded-lg text-destructive">
            <Trash2 className="h-5 w-5" />
          </div>
        ) : (
          <div className="p-2 bg-primary/10 rounded-lg text-primary">
            <Shield className="h-5 w-5" />
          </div>
        )}
        <div className="flex-1">
          <h3 className="font-semibold text-lg">{action.title}</h3>
          <p className="text-sm text-muted-foreground">{action.description}</p>
        </div>
      </div>

      {/* Fields */}
      <div className="border-t border-border pt-4">
        <h4 className="text-sm font-medium text-muted-foreground mb-3">Details</h4>
        <dl className="grid grid-cols-2 gap-2 text-sm">
          {action.fields.map((field, index) => (
            <motion.div
              key={index}
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: index * 0.05 }}
              className="col-span-2 flex items-baseline gap-2"
            >
              <dt className="text-muted-foreground w-32 shrink-0">{field.label}:</dt>
              <dd className="font-medium text-foreground break-all">{field.value}</dd>
            </motion.div>
          ))}
        </dl>
      </div>

      {/* Warning for destructive actions */}
      {action.destructive && (
        <motion.div
          initial={{ opacity: 0, height: 0 }}
          animate={{ opacity: 1, height: 'auto' }}
          className="p-3 bg-destructive/10 border border-destructive/20 rounded-lg flex items-start gap-2"
        >
          <AlertTriangle className="h-5 w-5 text-destructive flex-shrink-0 mt-0.5" />
          <div>
            <p className="font-medium text-destructive">This action cannot be undone</p>
            <p className="text-sm text-muted-foreground mt-1">
              Please confirm you want to proceed with this destructive action.
            </p>
          </div>
        </motion.div>
      )}

      {/* Actions */}
      <div className="flex items-center justify-end gap-3 border-t border-border pt-4">
        <button
          onClick={onReject}
          className="px-4 py-2 text-sm font-medium text-muted-foreground hover:text-foreground transition-colors"
        >
          Cancel
        </button>
        <button
          onClick={() => onConfirm({ confirmed: true, ...action })}
          className={cn(
            'px-4 py-2 text-sm font-medium rounded-lg transition-colors',
            action.destructive
              ? 'bg-destructive text-destructive-foreground hover:bg-destructive/90'
              : 'bg-primary text-primary-foreground hover:bg-primary/90'
          )}
          disabled={action.requiresConfirmation === false}
        >
          {action.destructive ? 'Confirm Delete' : 'Confirm'}
        </button>
      </div>
    </motion.div>
  );
}