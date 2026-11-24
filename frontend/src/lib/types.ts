/** DTO shapes returned by the Practice Manager student API. */

export interface Me {
  id: string;
  fullName: string;
  shortName: string;
  group: string;
  specialtyCode: string;
  specialtyName: string;
  course: number;
  studentNumber: string;
  phone: string | null;
  email: string | null;
}

export interface PersonInfo {
  shortName: string;
  fullName: string;
  position: string | null;
}

export interface OrganizationInfo {
  name: string;
  address: string | null;
  inn: string | null;
}

export interface StageInfo {
  name: string;
  status: string;
  statusLabel: string;
}

export interface DeadlineInfo {
  date: string;
  title: string;
  daysLeft: number;
}

export interface AssessmentInfo {
  grade: string | null;
  result: string;
  resultLabel: string;
  date: string | null;
  comment: string | null;
}

export interface PracticeCurrent {
  participantId: string;
  practiceName: string;
  practiceType: string;
  professionalModule: string;
  specialtyCode: string;
  specialtyName: string;
  dateStart: string;
  dateEnd: string;
  organization: OrganizationInfo;
  collegeSupervisor: PersonInfo;
  organizationSupervisor: PersonInfo;
  status: string;
  statusLabel: string;
  progressPercent: number;
  currentStage: StageInfo | null;
  stages: StageInfo[];
  assessment: AssessmentInfo | null;
  deadline: DeadlineInfo | null;
  unreadNotifications: number;
}

export interface PracticeHistoryItem {
  participantId: string;
  practiceName: string;
  professionalModule: string;
  dateStart: string;
  dateEnd: string;
  organization: string;
  status: string;
  statusLabel: string;
  grade: string | null;
  resultLabel: string;
}

export interface PracticesResponse {
  current: PracticeCurrent | null;
  history: PracticeHistoryItem[];
}

export interface TaskItem {
  id: string;
  title: string;
  description: string | null;
  deadline: string;
  status: string;
  statusLabel: string;
  teacherComment: string | null;
  isGroup: boolean;
}

export interface DiaryEntry {
  id: string;
  date: string;
  workDescription: string;
  supervisorMark: string | null;
}

export interface DiaryEntryInput {
  date: string;
  workDescription: string;
  supervisorMark: string | null;
}

export interface TeacherCommentDto {
  id: string;
  documentId: string;
  versionNumber: number | null;
  authorName: string;
  authorRole: string;
  kind: "return" | "acceptance" | "clarification";
  summary: string;
  fullText: string;
  createdAt: string;
}

export interface DocumentListItem {
  id: string;
  name: string;
  typeCode: string;
  typeName: string;
  status: string;
  statusLabel: string;
  updatedAt: string | null;
  teacherComment: string | null;
  currentVersionNumber: number | null;
  canGenerate: boolean;
  canUpload: boolean;
  canSubmit: boolean;
  canDelete: boolean;
}

export interface DocumentVersion {
  id: string;
  versionNumber: number;
  source: string;
  sourceLabel: string;
  fileName: string;
  sizeBytes: number;
  createdAt: string;
  teacherComment: string | null;
  downloadUrl: string;
}

export interface DocumentDetail {
  id: string;
  name: string;
  typeCode: string;
  typeName: string;
  status: string;
  statusLabel: string;
  content: string | null;
  updatedAt: string | null;
  teacherComment: string | null;
  versions: DocumentVersion[];
  canGenerate: boolean;
  canUpload: boolean;
  canSubmit: boolean;
  previewUrl: string | null;
  downloadUrl: string | null;
  comments: TeacherCommentDto[];
  canDelete: boolean;
}

export interface GenerateDocumentResponse {
  document: DocumentDetail;
  warnings: string[];
}

export interface DocumentTemplate {
  id: string;
  code: string;
  name: string;
  /** Backward-compatible field used by the original API. */
  documentTypeCode?: string;
  /** Revision 1 may return `documentType` instead of `documentTypeCode`. */
  documentType?: string;
  professionalModule: string | null;
  version: number;
  fileName?: string;
  sizeBytes?: number;
  downloadUrl?: string;
  isActive?: boolean;
}

export interface NotificationItem {
  id: string;
  title: string;
  message: string;
  isRead: boolean;
  link: string | null;
  createdAt: string;
}

export interface ApiErrorBody {
  detail?: string;
  title?: string;
  traceId?: string;
}
