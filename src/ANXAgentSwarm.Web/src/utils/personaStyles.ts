/**
 * Persona styling utilities for consistent UI appearance.
 * Maps persona types to Tailwind CSS color classes.
 */

import { PersonaType, MessageType, SessionStatus } from '@/types'

/**
 * Complete style definition for a persona.
 */
export interface PersonaStyle {
  /** Background color class for cards/badges */
  bgColor: string
  /** Light background for subtle highlights */
  bgLight: string
  /** Border color class */
  borderColor: string
  /** Text color class */
  textColor: string
  /** Ring/focus color */
  ringColor: string
  /** Icon for the persona */
  icon: string
  /** Gradient for avatar/header */
  gradient: string
}

/**
 * Style definition for session status.
 */
export interface StatusStyle {
  bgColor: string
  textColor: string
  borderColor: string
  icon: string
  pulseColor?: string
}

/**
 * Style definition for message types.
 */
export interface MessageTypeStyle {
  icon: string
  label: string
  bgColor: string
  textColor: string
}

/**
 * Get Tailwind styles for a persona type.
 */
export function getPersonaStyle(personaType: PersonaType): PersonaStyle {
  const styles: Record<PersonaType, PersonaStyle> = {
    [PersonaType.Coordinator]: {
      bgColor: 'bg-purple-500',
      bgLight: 'bg-purple-50',
      borderColor: 'border-purple-300',
      textColor: 'text-purple-700',
      ringColor: 'ring-purple-500',
      icon: '🎯',
      gradient: 'from-purple-500 to-purple-600'
    },
    [PersonaType.BusinessAnalyst]: {
      bgColor: 'bg-blue-500',
      bgLight: 'bg-blue-50',
      borderColor: 'border-blue-300',
      textColor: 'text-blue-700',
      ringColor: 'ring-blue-500',
      icon: '📊',
      gradient: 'from-blue-500 to-blue-600'
    },
    [PersonaType.TechnicalArchitect]: {
      bgColor: 'bg-indigo-500',
      bgLight: 'bg-indigo-50',
      borderColor: 'border-indigo-300',
      textColor: 'text-indigo-700',
      ringColor: 'ring-indigo-500',
      icon: '🏗️',
      gradient: 'from-indigo-500 to-indigo-600'
    },
    [PersonaType.SeniorDeveloper]: {
      bgColor: 'bg-green-500',
      bgLight: 'bg-green-50',
      borderColor: 'border-green-300',
      textColor: 'text-green-700',
      ringColor: 'ring-green-500',
      icon: '💻',
      gradient: 'from-green-500 to-green-600'
    },
    [PersonaType.JuniorDeveloper]: {
      bgColor: 'bg-emerald-500',
      bgLight: 'bg-emerald-50',
      borderColor: 'border-emerald-300',
      textColor: 'text-emerald-700',
      ringColor: 'ring-emerald-500',
      icon: '🌱',
      gradient: 'from-emerald-500 to-emerald-600'
    },
    [PersonaType.SeniorQA]: {
      bgColor: 'bg-orange-500',
      bgLight: 'bg-orange-50',
      borderColor: 'border-orange-300',
      textColor: 'text-orange-700',
      ringColor: 'ring-orange-500',
      icon: '🔍',
      gradient: 'from-orange-500 to-orange-600'
    },
    [PersonaType.JuniorQA]: {
      bgColor: 'bg-amber-500',
      bgLight: 'bg-amber-50',
      borderColor: 'border-amber-300',
      textColor: 'text-amber-700',
      ringColor: 'ring-amber-500',
      icon: '🐛',
      gradient: 'from-amber-500 to-amber-600'
    },
    [PersonaType.UXEngineer]: {
      bgColor: 'bg-pink-500',
      bgLight: 'bg-pink-50',
      borderColor: 'border-pink-300',
      textColor: 'text-pink-700',
      ringColor: 'ring-pink-500',
      icon: '🎨',
      gradient: 'from-pink-500 to-pink-600'
    },
    [PersonaType.UIEngineer]: {
      bgColor: 'bg-rose-500',
      bgLight: 'bg-rose-50',
      borderColor: 'border-rose-300',
      textColor: 'text-rose-700',
      ringColor: 'ring-rose-500',
      icon: '✨',
      gradient: 'from-rose-500 to-rose-600'
    },
    [PersonaType.DocumentWriter]: {
      bgColor: 'bg-teal-500',
      bgLight: 'bg-teal-50',
      borderColor: 'border-teal-300',
      textColor: 'text-teal-700',
      ringColor: 'ring-teal-500',
      icon: '📝',
      gradient: 'from-teal-500 to-teal-600'
    },
    [PersonaType.User]: {
      bgColor: 'bg-slate-500',
      bgLight: 'bg-slate-50',
      borderColor: 'border-slate-300',
      textColor: 'text-slate-700',
      ringColor: 'ring-slate-500',
      icon: '👤',
      gradient: 'from-slate-500 to-slate-600'
    }
  }

  return styles[personaType] ?? styles[PersonaType.User]
}

/**
 * Get display name for a persona type.
 */
export function getPersonaDisplayName(personaType: PersonaType): string {
  const names: Record<PersonaType, string> = {
    [PersonaType.Coordinator]: 'Coordinator',
    [PersonaType.BusinessAnalyst]: 'Business Analyst',
    [PersonaType.TechnicalArchitect]: 'Technical Architect',
    [PersonaType.SeniorDeveloper]: 'Senior Developer',
    [PersonaType.JuniorDeveloper]: 'Junior Developer',
    [PersonaType.SeniorQA]: 'Senior QA',
    [PersonaType.JuniorQA]: 'Junior QA',
    [PersonaType.UXEngineer]: 'UX Engineer',
    [PersonaType.UIEngineer]: 'UI Engineer',
    [PersonaType.DocumentWriter]: 'Document Writer',
    [PersonaType.User]: 'User'
  }

  return names[personaType] ?? 'Unknown'
}

/**
 * Get short name for a persona type.
 */
export function getPersonaShortName(personaType: PersonaType): string {
  const shortNames: Record<PersonaType, string> = {
    [PersonaType.Coordinator]: 'CO',
    [PersonaType.BusinessAnalyst]: 'BA',
    [PersonaType.TechnicalArchitect]: 'TA',
    [PersonaType.SeniorDeveloper]: 'Sr.Dev',
    [PersonaType.JuniorDeveloper]: 'Jr.Dev',
    [PersonaType.SeniorQA]: 'Sr.QA',
    [PersonaType.JuniorQA]: 'Jr.QA',
    [PersonaType.UXEngineer]: 'UX',
    [PersonaType.UIEngineer]: 'UI',
    [PersonaType.DocumentWriter]: 'Doc',
    [PersonaType.User]: 'User'
  }

  return shortNames[personaType] ?? '??'
}

/**
 * Get role description for a persona type.
 */
export function getPersonaRole(personaType: PersonaType): string {
  const roles: Record<PersonaType, string> = {
    [PersonaType.Coordinator]: 'Orchestrator',
    [PersonaType.BusinessAnalyst]: 'Requirements',
    [PersonaType.TechnicalArchitect]: 'Design',
    [PersonaType.SeniorDeveloper]: 'Implementation',
    [PersonaType.JuniorDeveloper]: 'Implementation',
    [PersonaType.SeniorQA]: 'Quality',
    [PersonaType.JuniorQA]: 'Quality',
    [PersonaType.UXEngineer]: 'Experience',
    [PersonaType.UIEngineer]: 'Interface',
    [PersonaType.DocumentWriter]: 'Documentation',
    [PersonaType.User]: 'User'
  }

  return roles[personaType] ?? 'Unknown'
}

/**
 * Get styles for session status.
 */
export function getStatusStyle(status: SessionStatus): StatusStyle {
  const styles: Record<SessionStatus, StatusStyle> = {
    [SessionStatus.Active]: {
      bgColor: 'bg-green-100',
      textColor: 'text-green-800',
      borderColor: 'border-green-300',
      icon: '🔄',
      pulseColor: 'bg-green-400'
    },
    [SessionStatus.WaitingForClarification]: {
      bgColor: 'bg-yellow-100',
      textColor: 'text-yellow-800',
      borderColor: 'border-yellow-300',
      icon: '❓',
      pulseColor: 'bg-yellow-400'
    },
    [SessionStatus.Completed]: {
      bgColor: 'bg-blue-100',
      textColor: 'text-blue-800',
      borderColor: 'border-blue-300',
      icon: '✅'
    },
    [SessionStatus.Stuck]: {
      bgColor: 'bg-orange-100',
      textColor: 'text-orange-800',
      borderColor: 'border-orange-300',
      icon: '⚠️'
    },
    [SessionStatus.Cancelled]: {
      bgColor: 'bg-gray-100',
      textColor: 'text-gray-800',
      borderColor: 'border-gray-300',
      icon: '🚫'
    },
    [SessionStatus.Error]: {
      bgColor: 'bg-red-100',
      textColor: 'text-red-800',
      borderColor: 'border-red-300',
      icon: '❌'
    }
  }

  return styles[status] ?? styles[SessionStatus.Error]
}

/**
 * Get status display name.
 */
export function getStatusDisplayName(status: SessionStatus): string {
  const names: Record<SessionStatus, string> = {
    [SessionStatus.Active]: 'Active',
    [SessionStatus.WaitingForClarification]: 'Waiting for Input',
    [SessionStatus.Completed]: 'Completed',
    [SessionStatus.Stuck]: 'Stuck',
    [SessionStatus.Cancelled]: 'Cancelled',
    [SessionStatus.Error]: 'Error'
  }

  return names[status] ?? 'Unknown'
}

/**
 * Get styles for message types.
 */
export function getMessageTypeStyle(messageType: MessageType): MessageTypeStyle {
  const styles: Record<MessageType, MessageTypeStyle> = {
    [MessageType.Question]: {
      icon: '❓',
      label: 'Question',
      bgColor: 'bg-blue-50',
      textColor: 'text-blue-700'
    },
    [MessageType.Answer]: {
      icon: '💬',
      label: 'Answer',
      bgColor: 'bg-green-50',
      textColor: 'text-green-700'
    },
    [MessageType.Delegation]: {
      icon: '➡️',
      label: 'Delegation',
      bgColor: 'bg-purple-50',
      textColor: 'text-purple-700'
    },
    [MessageType.Clarification]: {
      icon: '🤔',
      label: 'Clarification Needed',
      bgColor: 'bg-yellow-50',
      textColor: 'text-yellow-700'
    },
    [MessageType.Solution]: {
      icon: '✅',
      label: 'Solution',
      bgColor: 'bg-emerald-50',
      textColor: 'text-emerald-700'
    },
    [MessageType.Stuck]: {
      icon: '⚠️',
      label: 'Stuck',
      bgColor: 'bg-orange-50',
      textColor: 'text-orange-700'
    },
    [MessageType.ProblemStatement]: {
      icon: '📋',
      label: 'Problem Statement',
      bgColor: 'bg-slate-50',
      textColor: 'text-slate-700'
    },
    [MessageType.UserResponse]: {
      icon: '👤',
      label: 'User Response',
      bgColor: 'bg-slate-50',
      textColor: 'text-slate-700'
    },
    [MessageType.Decline]: {
      icon: '🚫',
      label: 'Declined',
      bgColor: 'bg-red-50',
      textColor: 'text-red-700'
    }
  }

  return styles[messageType] ?? styles[MessageType.Answer]
}

/**
 * Parse a timestamp string as UTC.
 * The backend returns UTC timestamps without the 'Z' suffix,
 * so we need to append it to ensure correct parsing.
 */
function parseUtcTimestamp(timestamp: string): Date {
  // If the timestamp doesn't have timezone info, treat it as UTC
  if (!timestamp.endsWith('Z') && !timestamp.includes('+') && !timestamp.includes('-', 10)) {
    return new Date(timestamp + 'Z')
  }
  return new Date(timestamp)
}

/**
 * Format a timestamp for display.
 */
export function formatTimestamp(timestamp: string): string {
  const date = parseUtcTimestamp(timestamp)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffMins = Math.floor(diffMs / 60000)
  const diffHours = Math.floor(diffMins / 60)
  const diffDays = Math.floor(diffHours / 24)

  if (diffMins < 1) {
    return 'Just now'
  } else if (diffMins < 60) {
    return `${diffMins}m ago`
  } else if (diffHours < 24) {
    return `${diffHours}h ago`
  } else if (diffDays === 1) {
    return 'Yesterday'
  } else if (diffDays < 7) {
    return `${diffDays}d ago`
  } else {
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: date.getFullYear() !== now.getFullYear() ? 'numeric' : undefined
    })
  }
}

/**
 * Format a timestamp with time.
 */
export function formatTimestampWithTime(timestamp: string): string {
  const date = parseUtcTimestamp(timestamp)
  return date.toLocaleString('en-US', {
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
    hour12: true
  })
}
