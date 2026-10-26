'use client';

import { cn } from '@/lib/utils';
import { motion } from 'framer-motion';
import { X, ChevronDown, User, Building, FolderKanban, CheckSquare, Calendar, AlertTriangle } from 'lucide-react';
import { ClarificationField, ChatMessage } from '@/types/chat';

interface ClarificationDialogProps {
  fields: ClarificationField[];
  originalIntent: ChatMessage['metadata'];
  onSubmit: (fields: Record<string, any>, originalIntent: any) => void;
}

export function ClarificationDialog({ fields, originalIntent, onSubmit }: ClarificationDialogProps) {
  const [values, setValues] = useState<Record<string, any>>({});
  const [errors, setErrors] = useState<Record<string, string>>({});

  const handleChange = (key: string, value: any) => {
    setValues(prev => ({ ...prev, [key]: value }));
    if (errors[key]) {
      setErrors(prev => { const next = { ...prev }; delete next[key]; return next; });
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    // Validate required fields
    const newErrors: Record<string, string> = {};
    fields.forEach(field => {
      if (field.required && (!values[field.key] || values[field.key] === '')) {
        newErrors[field.key] = `${field.label} is required`;
      }
    });
    
    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    onSubmit(values, originalIntent);
  };

  const getFieldIcon = (type: string) => {
    switch (type) {
      case 'user': return User;
      case 'project': return FolderKanban;
      case 'department': return Building;
      case 'task': return CheckSquare;
      case 'date': return Calendar;
      default: return AlertTriangle;
    }
  };

  return (
    <motion.form
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -20 }}
      onSubmit={handleSubmit}
      className="p-4 bg-card border border-border rounded-xl shadow-lg space-y-4"
    >
      <div className="flex items-center justify-between">
        <h3 className="font-semibold">Need More Information</h3>
        <button
          type="button"
          onClick={() => onSubmit({}, originalIntent)} // Cancel
          className="p-1 hover:bg-accent rounded-lg transition-colors"
          aria-label="Cancel"
        >
          <X className="h-5 w-5 text-muted-foreground" />
        </button>
      </div>

      <p className="text-sm text-muted-foreground">
        Please provide the missing details to proceed.
      </p>

      <div className="space-y-4">
        {fields.map((field, index) => (
          <motion.div
            key={field.key}
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: index * 0.05 }}
            className="space-y-1.5"
          >
            <label className="flex items-center gap-2 text-sm font-medium">
              {(() => {
                const Icon = getFieldIcon(field.type);
                return <Icon className="h-4 w-4 text-muted-foreground" />;
              })()}
              <span>{field.label}</span>
              {field.required && <span className="text-destructive">*</span>}
            </label>
            
            {field.type === 'select' && field.options ? (
              <select
                value={values[field.key] || ''}
                onChange={(e) => handleChange(field.key, e.target.value)}
                className="w-full px-3 py-2 bg-input border border-border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-transparent"
              >
                <option value="">Select...</option>
                {field.options.map(opt => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            ) : field.type === 'date' ? (
              <input
                type="date"
                value={values[field.key] || ''}
                onChange={(e) => handleChange(field.key, e.target.value)}
                className="w-full px-3 py-2 bg-input border border-border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-transparent"
                min={new Date().toISOString().split('T')[0]}
              />
            ) : field.type === 'user' || field.type === 'project' || field.type === 'department' ? (
              <input
                type="text"
                value={values[field.key] || ''}
                onChange={(e) => handleChange(field.key, e.target.value)}
                placeholder={`Search ${field.label.toLowerCase()}...`}
                className="w-full px-3 py-2 bg-input border border-border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-transparent"
                list={`${field.key}-suggestions`}
              />
            ) : (
              <input
                type="text"
                value={values[field.key] || ''}
                onChange={(e) => handleChange(field.key, e.target.value)}
                placeholder={`Enter ${field.label.toLowerCase()}`}
                className="w-full px-3 py-2 bg-input border border-border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-transparent"
                aria-invalid={!!errors[field.key]}
              />
            )}
            
            {errors[field.key] && (
              <p className="text-sm text-destructive flex items-center gap-1">
                <AlertTriangle className="h-3 w-3" />
                {errors[field.key]}
              </p>
            )}
            
            {field.type === 'user' && (
              <datalist id={`${field.key}-suggestions`}>
                <option value="@john.doe" />
                <option value="@jane.smith" />
                <option value="@bob.wilson" />
              </datalist>
            )}
            {field.type === 'project' && (
              <datalist id={`${field.key}-suggestions`}>
                <option value="API Refactoring" />
                <option value="Database Migration" />
                <option value="Security Audit" />
              </datalist>
            )}
            {field.type === 'department' && (
              <datalist id={`${field.key}-suggestions`}>
                <option value="Engineering" />
                <option value="Marketing" />
                <option value="Sales" />
                <option value="Operations" />
              </datalist>
            )}
          </motion.div>
        ))}
      </div>

      <div className="flex justify-end gap-3 pt-4 border-t border-border">
        <button
          type="button"
          onClick={() => onSubmit({}, originalIntent)}
          className="px-4 py-2 text-sm font-medium text-muted-foreground hover:text-foreground transition-colors"
        >
          Cancel
        </button>
        <button
          type="submit"
          className="px-4 py-2 text-sm font-medium bg-primary text-primary-foreground rounded-lg hover:bg-primary/90 transition-colors"
        >
          Continue
        </button>
      </div>
    </motion.form>
  );
}

// Need to add useState import
import { useState } from 'react';