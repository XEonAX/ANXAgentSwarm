/**
 * TypeScript types matching backend DTOs for ANXAgentSwarm
 */

// ============================================================================
// Enums
// ============================================================================

/**
 * Represents the different persona types (agents) available in the system.
 */
export enum PersonaType {
  /** Central orchestrator - receives problems, plans approach, compiles solutions. */
  Coordinator = 0,
  /** Requirements specialist - gathers requirements, analyzes business needs. */
  BusinessAnalyst = 1,
  /** System design expert - architecture decisions, technology selection. */
  TechnicalArchitect = 2,
  /** Expert implementer - complex features, code review, mentoring. */
  SeniorDeveloper = 3,
  /** Eager learner - basic features, follows patterns, learning-oriented. */
  JuniorDeveloper = 4,
  /** Quality strategy expert - test strategy, complex test cases. */
  SeniorQA = 5,
  /** Test executor - test execution, bug reporting. */
  JuniorQA = 6,
  /** User experience designer - user flows, wireframes, usability. */
  UXEngineer = 7,
  /** Visual designer - visual interfaces, component styling. */
  UIEngineer = 8,
  /** Technical writer - documentation, user guides, API docs. */
  DocumentWriter = 9,
  /** Represents the user (for messages from the user). */
  User = 99
}

/**
 * Represents the current status of a session.
 */
export enum SessionStatus {
  /** Session is actively being processed. */
  Active = 0,
  /** Session is waiting for user clarification. */
  WaitingForClarification = 1,
  /** Session has been completed successfully with a solution. */
  Completed = 2,
  /** All personas are stuck and cannot proceed. */
  Stuck = 3,
  /** Session was cancelled by the user. */
  Cancelled = 4,
  /** Session encountered an error. */
  Error = 5
}

/**
 * Represents the type of message in a conversation.
 */
export enum MessageType {
  /** A question being asked (to another persona or to the user). */
  Question = 0,
  /** An answer to a previous question. */
  Answer = 1,
  /** A delegation to another persona with context. */
  Delegation = 2,
  /** A request for clarification from the user. */
  Clarification = 3,
  /** A complete or partial solution. */
  Solution = 4,
  /** The persona is stuck and cannot proceed. */
  Stuck = 5,
  /** Initial problem statement from the user. */
  ProblemStatement = 6,
  /** User's response to a clarification request. */
  UserResponse = 7,
  /** A decline to handle a delegated task. */
  Decline = 8
}

// ============================================================================
// DTOs
// ============================================================================

/**
 * Request to create a new session.
 */
export interface CreateSessionRequest {
  problemStatement: string
}

/**
 * Request to submit a clarification response.
 */
export interface ClarificationResponse {
  response: string
}

/**
 * DTO for Session entity (list view).
 */
export interface SessionDto {
  id: string
  title: string
  status: SessionStatus
  problemStatement: string
  finalSolution: string | null
  createdAt: string
  updatedAt: string
  messageCount: number
  currentPersona: PersonaType | null
}

/**
 * DTO for Session with messages (detail view).
 */
export interface SessionDetailDto {
  id: string
  title: string
  status: SessionStatus
  problemStatement: string
  finalSolution: string | null
  createdAt: string
  updatedAt: string
  currentPersona: PersonaType | null
  messages: MessageDto[]
}

/**
 * DTO for Message entity.
 */
export interface MessageDto {
  id: string
  fromPersona: PersonaType
  toPersona: PersonaType | null
  content: string
  messageType: MessageType
  internalReasoning: string | null
  timestamp: string
  parentMessageId: string | null
  delegateToPersona: PersonaType | null
  delegationContext: string | null
  isStuck: boolean
}

/**
 * DTO for Memory entity.
 */
export interface MemoryDto {
  id: string
  personaType: PersonaType
  identifier: string
  content: string
  createdAt: string
  accessCount: number
}

/**
 * DTO for PersonaConfiguration entity.
 */
export interface PersonaConfigurationDto {
  id: string
  personaType: PersonaType
  displayName: string
  modelName: string
  temperature: number
  maxTokens: number
  isEnabled: boolean
  description: string | null
}

/**
 * Response for LLM availability check.
 */
export interface LlmStatusDto {
  isAvailable: boolean
  availableModels: string[]
  defaultModel: string
}

// ============================================================================
// SignalR Event Types
// ============================================================================

/**
 * Event payload when a new message is received.
 */
export interface MessageReceivedEvent {
  sessionId: string
  message: MessageDto
}

/**
 * Event payload when session status changes.
 */
export interface SessionStatusChangedEvent {
  sessionId: string
  status: SessionStatus
  currentPersona: PersonaType | null
}

/**
 * Event payload when clarification is requested.
 */
export interface ClarificationRequestedEvent {
  sessionId: string
  question: string
  fromPersona: PersonaType
}

/**
 * Event payload when solution is ready.
 */
export interface SolutionReadyEvent {
  sessionId: string
  solution: string
}

/**
 * Event payload when session is stuck.
 */
export interface SessionStuckEvent {
  sessionId: string
  partialResults: string
  reason: string
}

// ============================================================================
// Utility Types
// ============================================================================

/**
 * Persona metadata for display purposes.
 */
export interface PersonaInfo {
  type: PersonaType
  displayName: string
  shortName: string
  role: string
  color: string
  icon: string
}

/**
 * Session status metadata for display purposes.
 */
export interface SessionStatusInfo {
  status: SessionStatus
  displayName: string
  color: string
  icon: string
}

/**
 * Helper function to get persona display information.
 */
export function getPersonaInfo(personaType: PersonaType): PersonaInfo {
  const personaMap: Record<PersonaType, Omit<PersonaInfo, 'type'>> = {
    [PersonaType.Coordinator]: {
      displayName: 'Coordinator',
      shortName: 'CO',
      role: 'Orchestrator',
      color: 'purple',
      icon: '🎯'
    },
    [PersonaType.BusinessAnalyst]: {
      displayName: 'Business Analyst',
      shortName: 'BA',
      role: 'Requirements',
      color: 'blue',
      icon: '📊'
    },
    [PersonaType.TechnicalArchitect]: {
      displayName: 'Technical Architect',
      shortName: 'TA',
      role: 'Design',
      color: 'indigo',
      icon: '🏗️'
    },
    [PersonaType.SeniorDeveloper]: {
      displayName: 'Senior Developer',
      shortName: 'SrDev',
      role: 'Implementation',
      color: 'green',
      icon: '💻'
    },
    [PersonaType.JuniorDeveloper]: {
      displayName: 'Junior Developer',
      shortName: 'JrDev',
      role: 'Implementation',
      color: 'emerald',
      icon: '🌱'
    },
    [PersonaType.SeniorQA]: {
      displayName: 'Senior QA',
      shortName: 'SrQA',
      role: 'Quality',
      color: 'orange',
      icon: '🔍'
    },
    [PersonaType.JuniorQA]: {
      displayName: 'Junior QA',
      shortName: 'JrQA',
      role: 'Quality',
      color: 'amber',
      icon: '🐛'
    },
    [PersonaType.UXEngineer]: {
      displayName: 'UX Engineer',
      shortName: 'UX',
      role: 'Experience',
      color: 'pink',
      icon: '🎨'
    },
    [PersonaType.UIEngineer]: {
      displayName: 'UI Engineer',
      shortName: 'UI',
      role: 'Interface',
      color: 'rose',
      icon: '✨'
    },
    [PersonaType.DocumentWriter]: {
      displayName: 'Document Writer',
      shortName: 'Doc',
      role: 'Documentation',
      color: 'teal',
      icon: '📝'
    },
    [PersonaType.User]: {
      displayName: 'User',
      shortName: 'User',
      role: 'User',
      color: 'gray',
      icon: '👤'
    }
  }

  return {
    type: personaType,
    ...personaMap[personaType]
  }
}

/**
 * Helper function to get session status display information.
 */
export function getSessionStatusInfo(status: SessionStatus): SessionStatusInfo {
  const statusMap: Record<SessionStatus, Omit<SessionStatusInfo, 'status'>> = {
    [SessionStatus.Active]: {
      displayName: 'Active',
      color: 'green',
      icon: '🔄'
    },
    [SessionStatus.WaitingForClarification]: {
      displayName: 'Waiting for Input',
      color: 'yellow',
      icon: '❓'
    },
    [SessionStatus.Completed]: {
      displayName: 'Completed',
      color: 'blue',
      icon: '✅'
    },
    [SessionStatus.Stuck]: {
      displayName: 'Stuck',
      color: 'orange',
      icon: '⚠️'
    },
    [SessionStatus.Cancelled]: {
      displayName: 'Cancelled',
      color: 'gray',
      icon: '🚫'
    },
    [SessionStatus.Error]: {
      displayName: 'Error',
      color: 'red',
      icon: '❌'
    }
  }

  return {
    status,
    ...statusMap[status]
  }
}
