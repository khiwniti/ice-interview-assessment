'use client';

import { useState, useEffect, useRef, useCallback } from 'react';
import { useChatStore } from '@/lib/store/chat-store';
import { getAIOrchestrator } from '@/lib/ai/orchestrator';
import { getContextManager } from '@/lib/ai/context-manager';
import { apiClient } from '@/lib/api/client';
import { MessageBubble } from './MessageBubble';
import { ActionPreviewCard } from './ActionPreviewCard';
import { ClarificationDialog } from './ClarificationDialog';
import { ContextPanel } from './ContextPanel';
import { CommandPalette } from './CommandPalette';
import { ChatInput } from './ChatInput';
import { cn } from '@/lib/utils';
import { Send, Bot, Loader2, Sparkles, Menu, X, ChevronLeft, ChevronRight } from 'lucide-react';
import { motion, AnimatePresence } from 'framer-motion';

export function ChatInterface() {
  const {
    messages,
    addMessage,
    updateMessage,
    pendingAction,
    isStreaming,
    isSidebarOpen,
    setSidebarOpen,
    commandPaletteOpen,
    setCommandPaletteOpen,
    setPendingAction,
    context,
  } = useChatStore();

  const [input, setInput] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const chatContainerRef = useRef<HTMLDivElement>(null);
  const orchestrator = getAIOrchestrator();
  const contextManager = getContextManager();

  // Initialize on mount
  useEffect(() => {
    const init = async () => {
      const token = localStorage.getItem('auth_token');
      if (token) {
        apiClient.setToken(token);
        try {
          // Could fetch user context here
        } catch (e) {
          console.error('Failed to initialize context', e);
        }
      }
    };
    init();
  }, []);

  // Auto-scroll to bottom
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const handleSend = useCallback(async (message: string) => {
    if (!message.trim() || isLoading) return;

    // Add user message
    const userMessage = {
      id: crypto.randomUUID(),
      role: 'user' as const,
      content: message,
      timestamp: new Date(),
    };
    addMessage(userMessage);

    // Clear input
    setInput('');

    // Process through AI
    setIsLoading(true);
    try {
      const orchestrator = getAIOrchestrator();
      const response = await orchestrator.processMessage(message);

      // Add AI response
      const aiMessage = {
        id: crypto.randomUUID(),
        role: 'assistant' as const,
        content: response.message,
        timestamp: new Date(),
        type: response.type,
        metadata: response.metadata,
      };
      addMessage(aiMessage);

      // Handle action preview
      if (response.type === 'action_preview' && response.metadata?.previewData) {
        setPendingAction(response.metadata.previewData);
      }

      // Handle clarification
      if (response.type === 'clarification' && response.metadata?.clarificationFields) {
        // Clarification handled by ClarificationDialog component
      }
    } catch (error) {
      console.error('Chat error:', error);
      const errorMessage = {
        id: crypto.randomUUID(),
        role: 'assistant' as const,
        content: 'Sorry, I encountered an error. Please try again.',
        timestamp: new Date(),
        type: 'error' as const,
      };
      addMessage(errorMessage);
    } finally {
      setIsLoading(false);
    }
  }, [addMessage, setPendingAction, isLoading]);

  const handleActionConfirm = useCallback(async (actionId: string, intent: any) => {
    const orchestrator = getAIOrchestrator();
    setPendingAction(null);
    
    const response = await orchestrator.executeConfirmedAction(actionId, intent);
    
    const aiMessage = {
      id: crypto.randomUUID(),
      role: 'assistant' as const,
      content: response.message,
      timestamp: new Date(),
      type: response.type,
      metadata: response.metadata,
    };
    addMessage(aiMessage);
  }, [addMessage]);

  const handleActionReject = useCallback(() => {
    setPendingAction(null);
    addMessage({
      id: crypto.randomUUID(),
      role: 'assistant',
      content: 'Action cancelled.',
      timestamp: new Date(),
    });
  }, [addMessage, setPendingAction]);

  const handleClarificationSubmit = useCallback(async (fields: Record<string, any>, originalIntent: any) => {
    const orchestrator = getAIOrchestrator();
    const response = await orchestrator.handleClarification(fields, originalIntent);
    
    const aiMessage = {
      id: crypto.randomUUID(),
      role: 'assistant' as const,
      content: response.message,
      timestamp: new Date(),
      type: response.type,
      metadata: response.metadata,
    };
    addMessage(aiMessage);

    if (response.type === 'action_preview' && response.metadata?.previewData) {
      setPendingAction(response.metadata.previewData);
    }
  }, [addMessage, setPendingAction]);

  return (
    <div className="flex h-screen bg-background flex-col">
      {/* Header */}
      <header className="flex items-center justify-between px-4 py-3 border-b border-border bg-background/95 backdrop-blur-sm sticky top-0 z-10">
        <div className="flex items-center gap-4">
          <button
            onClick={() => setSidebarOpen(!isSidebarOpen)}
            className="lg:hidden p-2 rounded-lg hover:bg-accent transition-colors"
            aria-label="Toggle sidebar"
          >
            <Menu className="h-5 w-5" />
          </button>
          <div className="flex items-center gap-2">
            <Sparkles className="h-6 w-6 text-primary" />
            <span className="font-semibold text-lg">Business Analytics AI</span>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <CommandPalette 
            isOpen={commandPaletteOpen} 
            onClose={() => setCommandPaletteOpen(false)} 
            onSelect={(item) => {
              setCommandPaletteOpen(false);
              handleSend(item.action);
            }}
          />
          
          <button
            onClick={() => setCommandPaletteOpen(true)}
            className="p-2 rounded-lg hover:bg-accent transition-colors"
            aria-label="Command palette (⌘K)"
          >
            <span className="text-xs font-mono px-2 py-1 bg-muted rounded">⌘K</span>
          </button>

          <div className="hidden sm:flex items-center gap-2 px-3 py-1 bg-muted rounded-lg text-sm">
            <span className="text-muted-foreground">{context.userRole}</span>
            <span className="w-px h-4 bg-border mx-1" />
            <span>{context.userDepartment}</span>
          </div>
        </div>
      </header>

      <div className="flex-1 flex overflow-hidden">
        {/* Sidebar */}
        <AnimatePresence mode="wait">
          {isSidebarOpen && (
            <motion.aside
              initial={{ width: 0, opacity: 0 }}
              animate={{ width: '280px', opacity: 1 }}
              exit={{ width: 0, opacity: 0 }}
              className="flex flex-col border-r border-border bg-card overflow-hidden lg:w-72 flex-shrink-0"
            >
              <ContextPanel 
                context={context} 
                onClose={() => setSidebarOpen(false)} 
              />
            </motion.aside>
          )}
        </AnimatePresence>

        {/* Main Chat Area */}
        <main className="flex-1 flex flex-col min-w-0 relative">
          {/* Chat Messages */}
          <div
            ref={chatContainerRef}
            className="flex-1 overflow-y-auto p-4 space-y-4 scrollbar-hide"
            role="log"
            aria-live="polite"
            aria-label="Chat messages"
          >
            {messages.length === 0 ? (
              <div className="flex flex-col items-center justify-center h-full text-center text-muted-foreground py-12">
                <Sparkles className="h-12 w-12 mb-4 opacity-50" />
                <h3 className="text-lg font-medium mb-2">Welcome to Business Analytics AI</h3>
                <p className="max-w-xs mb-6">
                  I can help you manage projects, tasks, users, departments, and reports through natural language.
                </p>
                <div className="flex flex-wrap gap-2 justify-center">
                  <button
                    onClick={() => handleSend('Show me department progress')}
                    className="px-4 py-2 text-sm bg-primary/10 text-primary rounded-lg hover:bg-primary/20 transition-colors"
                  >
                    Show department progress
                  </button>
                  <button
                    onClick={() => handleSend('Create a new project')}
                    className="px-4 py-2 text-sm bg-primary/10 text-primary rounded-lg hover:bg-primary/20 transition-colors"
                  >
                    Create a project
                  </button>
                  <button
                    onClick={() => handleSend('Create a task')}
                    className="px-4 py-2 text-sm bg-primary/10 text-primary rounded-lg hover:bg-primary/20 transition-colors"
                  >
                    Create a task
                  </button>
                  <button
                    onClick={() => handleSend('Export report as CSV')}
                    className="px-4 py-2 text-sm bg-primary/10 text-primary rounded-lg hover:bg-primary/20 transition-colors"
                  >
                    Export report
                  </button>
                </div>
              </div>
            ) : (
              <>
                {messages.map((message) => (
                  <MessageBubble
                    key={message.id}
                    message={message}
                    onConfirm={handleActionConfirm}
                    onReject={handleActionReject}
                    onClarificationSubmit={handleClarificationSubmit}
                  />
                ))}
                
                {/* Action Preview Card */}
                {pendingAction && (
                  <ActionPreviewCard
                    action={pendingAction}
                    onConfirm={(intent) => handleActionConfirm(pendingAction.actionId, intent)}
                    onReject={handleActionReject}
                  />
                )}

                {/* Clarification Dialog */}
                {messages[messages.length - 1]?.metadata?.clarificationFields && (
                  <ClarificationDialog
                    fields={messages[messages.length - 1].metadata.clarificationFields!}
                    originalIntent={messages[messages.length - 1].metadata}
                    onSubmit={handleClarificationSubmit}
                  />
                )}

                <div ref={messagesEndRef} />
              </>
            )}

            {/* Streaming indicator */}
            {isStreaming && (
              <motion.div
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                className="flex items-center gap-2 text-muted-foreground text-sm px-4 py-2"
              >
                <Loader2 className="h-4 w-4 animate-spin text-primary" />
                <span>AI is thinking...</span>
              </motion.div>
            )}
          </div>

          {/* Chat Input */}
          <ChatInput
            value={input}
            onChange={setInput}
            onSend={handleSend}
            disabled={isLoading || isStreaming}
            placeholder="Type a message... (⌘K for commands)"
          />
        </main>
      </div>
    </div>
  );
}

// Import apiClient here to avoid circular dependency
import { apiClient } from '@/lib/api/client';