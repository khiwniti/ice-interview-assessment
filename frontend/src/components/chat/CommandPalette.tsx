'use client';

import { useState, useEffect, useRef, useCallback } from 'react';
import { cn } from '@/lib/utils';
import { motion, AnimatePresence } from 'framer-motion';
import { Command, CommandInput, CommandList, CommandItem, CommandGroup } from 'cmdk';
import { Sparkles, Search, BarChart, FileText, FolderKanban, CheckSquare, UserPlus, Building, Download, Activity, Database, RotateCcw, Keyboard } from 'lucide-react';
import { COMMAND_PALETTE_ITEMS } from '@/lib/store/chat-store';

interface CommandPaletteProps {
  isOpen: boolean;
  onClose: () => void;
  onSelect: (action: string) => void;
}

export function CommandPalette({ isOpen, onClose, onSelect }: CommandPaletteProps) {
  const [filter, setFilter] = useState('');
  const [selectedIndex, setSelectedIndex] = useState(0);
  const dialogRef = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault();
        onClose(); // Toggle - in real implementation would toggle
      }
      if (e.key === 'Escape') {
        onClose();
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [onClose]);

  // Filter items based on search
  const filteredItems = COMMAND_PALETTE_ITEMS
    .filter(item => 
      item.label.toLowerCase().includes(filter.toLowerCase()) ||
      item.description.toLowerCase().includes(filter.toLowerCase()) ||
      item.category.toLowerCase().includes(filter.toLowerCase())
    )
    .sort((a, b) => {
      // Prioritize exact matches
      const aExact = a.label.toLowerCase().startsWith(filter.toLowerCase());
      const bExact = b.label.toLowerCase().startsWith(filter.toLowerCase());
      if (aExact && !bExact) return -1;
      if (!aExact && bExact) return 1;
      return 0;
    });

  return (
    <AnimatePresence>
      {isOpen && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          className="fixed inset-0 z-50 flex items-start justify-center pt-20"
          onClick={onClose}
        >
          <motion.div
            initial={{ opacity: 0, y: -20, scale: 0.95 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: -20, scale: 0.95 }}
            className="w-full max-w-md mx-4 bg-card border border-border rounded-xl shadow-2xl overflow-hidden"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center gap-3 p-4 border-b border-border">
              <div className="flex-1 relative">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-muted-foreground" />
                <input
                  type="text"
                  placeholder="Type a command or search..."
                  value={filter}
                  onChange={(e) => setFilter(e.target.value)}
                  className="w-full pl-10 pr-4 py-2 bg-input border border-border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-transparent"
                  autoFocus
                />
              </div>
              <kbd className="px-2 py-1 text-xs font-mono bg-muted rounded">
                <Keyboard className="h-3 w-3" />
              </kbd>
            </div>

            <Command>
              <CommandInput value={filter} onValueChange={setFilter} />
              <CommandList className="max-h-96 overflow-auto">
                {filteredItems.length === 0 ? (
                  <div className="p-8 text-center text-muted-foreground">
                    No commands found
                  </div>
                ) : (
                  <>
                    {['project', 'task', 'user', 'report', 'system'].map(category => {
                      const items = filteredItems.filter(item => item.category === category);
                      if (items.length === 0) return null;
                      
                      return (
                        <CommandGroup key={category}>
                          <CommandItem className="px-3 py-1 text-xs font-semibold text-muted-foreground uppercase tracking-wider">
                            {category.charAt(0).toUpperCase() + category.slice(1)}
                          </CommandItem>
                          {items.map(item => (
                            <CommandItem
                              key={item.label}
                              onSelect={() => {
                                onSelect(item.action);
                                // Don't close - let user select multiple or close manually
                              }}
                              className="flex items-center gap-3 px-3 py-2"
                            >
                              <span className="flex items-center justify-center w-8 h-8 rounded-lg bg-muted">
                                {(() => {
                                  const icons: Record<string, any> = {
                                    Plus: () => <span className="h-4 w-4">+</span>,
                                    BarChart: () => <svg className="h-4 w-4"><rect x="1" y="1" width="4" height="12"/><rect x="7" y="4" width="4" height="9"/><rect x="13" y="7" width="4" height="6"/></svg>,
                                    FileText: () => <svg className="h-4 w-4"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8Z"/><polyline points="14 2 14 8 20 8"/></svg>,
                                    CheckSquare: () => <svg className="h-4 w-4"><rect x="1" y="1" width="14" height="14" rx="2"/><path d="M5 11l3 3 7-7"/></svg>,
                                    UserPlus: () => <svg className="h-4 w-4"><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><line x1="19" y1="8" x2="19" y2="14"/><line x1="22" y1="11" x2="17" y2="11"/></svg>,
                                    Building: () => <svg className="h-4 w-4"><rect x="1" y="1" width="22" height="22" rx="2"/><path d="M5 12h14"/><path d="M5 17h14"/></svg>,
                                    Download: () => <svg className="h-4 w-4"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>,
                                    Activity: () => <svg className="h-4 w-4"><polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/></svg>,
                                    Database: () => <svg className="h-4 w-4"><ellipse cx="12" cy="5" rx="9" ry="3"/><path d="M21 12c0 1.66-4 3-9 3s-9-1.34-9-3"/><path d="M3 5V19A9 3 0 0 0 21 19"/></svg>,
                                    RotateCcw: () => <svg className="h-4 w-4"><path d="M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8"/><path d="M3 3v5h5"/></svg>,
                                  };
                                  const Icon = icons[item.icon] || (() => <span>{item.icon}</span>);
                                  return <Icon className="h-4 w-4" />;
                                })()}
                              </span>
                              <div className="flex-1 text-left">
                                <span className="font-medium">{item.label}</span>
                                <span className="text-xs text-muted-foreground block">{item.description}</span>
                              </div>
                              <span className="text-xs font-mono px-2 py-0.5 bg-muted rounded">{item.shortcut || '⌘K'}</span>
                            </CommandItem>
                          ))}
                        </CommandGroup>
                      );
                    })}
                  </>
                )}
              </CommandList>
            </Command>

            <div className="p-4 border-t border-border text-center text-xs text-muted-foreground">
              <kbd className="px-1.5 py-0.5 bg-muted rounded">⌘K</kbd> to open • <kbd className="px-1.5 py-0.5 bg-muted rounded">Esc</kbd> to close
            </div>
          </motion.div>
        </motion.div>
      </AnimatePresence>
    </motion.div>
  );
}

// Need to add dialog element for portal
// In real implementation, would use a portal to body