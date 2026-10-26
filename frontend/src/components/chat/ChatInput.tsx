'use client';

import { useState, useRef, useEffect, useCallback } from 'react';
import { cn } from '@/lib/utils';
import { Send, Mic, Paperclip, Smile, MoreHorizontal } from 'lucide-react';

interface ChatInputProps {
  value: string;
  onChange: (value: string) => void;
  onSend: (message: string) => void;
  disabled?: boolean;
  placeholder?: string;
}

export function ChatInput({ value, onChange, onSend, disabled = false, placeholder = 'Type a message...' }: ChatInputProps) {
  const [isComposing, setIsComposing] = useState(false);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  // Auto-resize textarea
  useEffect(() => {
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
      textareaRef.current.style.height = Math.min(textareaRef.current.scrollHeight, 150) + 'px';
    }
  }, [value]);

  const handleSubmit = useCallback((e: React.FormEvent) => {
    e.preventDefault();
    if (value.trim() && !disabled && !isComposing) {
      onSend(value.trim());
    }
  }, [value, disabled, onSend, isComposing]);

  const handleKeyDown = useCallback((e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey && !isComposing) {
      e.preventDefault();
      if (value.trim() && !disabled) {
        onSend(value.trim());
      }
    }
  }, [value, disabled, onSend, isComposing]);

  const handleCompositionStart = () => setIsComposing(true);
  const handleCompositionEnd = (e: React.CompositionEvent) => {
    setIsComposing(false);
    onChange(e.currentTarget.value);
  };

  return (
    <form onSubmit={handleSubmit} className="w-full">
      <div className="flex items-end gap-2 p-4 border-t border-border bg-background/95 backdrop-blur-sm">
        {/* Attachment & Mic buttons */}
        <div className="flex items-center gap-1">
          <button
            type="button"
            className="p-2 rounded-lg hover:bg-accent transition-colors text-muted-foreground hover:text-foreground"
            aria-label="Attach file"
            disabled={disabled}
          >
            <Paperclip className="h-5 w-5" />
          </button>
          <button
            type="button"
            className="p-2 rounded-lg hover:bg-accent transition-colors text-muted-foreground hover:text-foreground"
            aria-label="Voice input"
            disabled={disabled}
          >
            <Mic className="h-5 w-5" />
          </button>
          <button
            type="button"
            className="p-2 rounded-lg hover:bg-accent transition-colors text-muted-foreground hover:text-foreground"
            aria-label="Emoji"
            disabled={disabled}
          >
            <Smile className="h-5 w-5" />
          </button>
        </div>

        {/* Textarea */}
        <div className="flex-1 relative">
          <textarea
            ref={textareaRef}
            value={value}
            onChange={(e) => onChange(e.target.value)}
            onKeyDown={handleKeyDown}
            onCompositionStart={handleCompositionStart}
            onCompositionEnd={handleCompositionEnd}
            placeholder={placeholder}
            disabled={disabled}
            rows={1}
            className={cn(
              'w-full px-4 py-3 bg-input border border-border rounded-xl text-sm',
              'focus:outline-none focus:ring-2 focus:ring-ring focus:border-transparent',
              'resize-none transition-all',
              disabled && 'opacity-50 cursor-not-allowed'
            )}
            style={{ minHeight: '44px', maxHeight: '150px' }}
            aria-label="Chat input"
          />
          
          {/* Character count when approaching limit */}
          {value.length > 1000 && (
            <span className="absolute bottom-2 right-2 text-xs text-muted-foreground">
              {value.length}/4000
            </span>
          )}
        </div>

        {/* Send button */}
        <button
          type="submit"
          disabled={disabled || !value.trim() || isComposing}
          className={cn(
            'p-2.5 rounded-xl transition-all',
            'flex-shrink-0',
            'bg-primary text-primary-foreground hover:bg-primary/90',
            'disabled:opacity-50 disabled:cursor-not-allowed',
            'focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2'
          )}
          aria-label="Send message"
        >
          <Send className="h-5 w-5" />
        </button>

        {/* More options */}
        <button
          type="button"
          className="p-2 rounded-lg hover:bg-accent transition-colors text-muted-foreground hover:text-foreground"
          aria-label="More options"
          disabled={disabled}
        >
          <MoreHorizontal className="h-5 w-5" />
        </button>
      </div>
    </form>
  );
}