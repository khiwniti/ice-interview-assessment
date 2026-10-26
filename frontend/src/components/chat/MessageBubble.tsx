'use client';

import { cn } from '@/lib/utils';
import { formatDistanceToNow } from 'date-fns';
import { Bot, User, Copy, Check, AlertCircle, Loader2 } from 'lucide-react';
import { motion } from 'framer-motion';
import { ChatMessage } from '@/types/chat';

interface MessageBubbleProps {
  message: ChatMessage;
  onConfirm?: (actionId: string, intent: any) => void;
  onReject?: () => void;
  onClarificationSubmit?: (fields: Record<string, any>, originalIntent: any) => void;
}

export function MessageBubble({ message, onConfirm, onReject, onClarificationSubmit }: MessageBubbleProps) {
  const isUser = message.role === 'user';
  const isError = message.type === 'error';
  const isActionPreview = message.type === 'action_preview';
  const isClarification = message.type === 'clarification';
  const isResult = message.type === 'result';

  if (isActionPreview || isClarification) {
    return null; // Handled by dedicated components
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      className={cn(
        'flex gap-3 max-w-[85%]',
        isUser ? 'justify-end' : 'justify-start'
      )}
    >
      {!isUser && (
        <div className="flex-shrink-0 w-8 h-8 rounded-full bg-primary/10 flex items-center justify-center text-primary">
          <Bot className="h-4 w-4" />
        </div>
      )}

      <div
        className={cn(
          'relative max-w-[75%] rounded-2xl px-4 py-3',
          isUser
            ? 'bg-primary text-primary-foreground rounded-tr-sm'
            : 'bg-card border border-border rounded-tl-sm',
          isError && 'bg-destructive/10 border-destructive/50 text-destructive'
        )}
      >
        <div className="prose prose-sm max-w-none">
          {message.content}
        </div>

        {/* Result data display */}
        {message.metadata?.resultData && (
          <motion.div
            initial={{ opacity: 0, height: 0 }}
            animate={{ opacity: 1, height: 'auto' }}
            className="mt-3 p-3 bg-muted/50 rounded-lg text-sm"
          >
            <pre className="whitespace-pre-wrap font-mono text-xs">
              {JSON.stringify(message.metadata.resultData, null, 2)}
            </pre>
          </motion.div>
        )}

        {/* Error display */}
        {message.metadata?.errorData && (
          <motion.div
            initial={{ opacity: 0, height: 0 }}
            animate={{ opacity: 1, height: 'auto' }}
            className="mt-3 p-3 bg-destructive/10 border border-destructive/20 rounded-lg text-sm text-destructive flex items-center gap-2"
          >
            <AlertCircle className="h-4 w-4 flex-shrink-0" />
            <span>{message.metadata.errorData.message || 'An error occurred'}</span>
          </motion.div>
        )}

        {/* Timestamp */}
        <div className="flex items-center justify-end gap-2 mt-2 text-xs text-muted-foreground">
          <time dateTime={message.timestamp.toISOString()}>
            {formatDistanceToNow(new Date(message.timestamp), { addSuffix: true })}
          </time>
        </div>
      </div>

      {isUser && (
        <div className="flex-shrink-0 w-8 h-8 rounded-full bg-muted flex items-center justify-center text-muted-foreground">
          <User className="h-4 w-4" />
        </div>
      )}
    </motion.div>
  );
}