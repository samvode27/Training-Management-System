import { Temporal } from "@js-temporal/polyfill";

export interface Course {
  id: number;
  code: string;
  title: string;
  maxCapacity: number;
  enrollmentCount: number;
  status?: string;
  category?: string;
  department?: string;
  credits?: number;
  summary?: string;
  description?: string;
  prerequisites?: string;
  learningOutcomesJson?: string;
  syllabusJson?: string;
  industrySkillsJson?: string;
  instructorId?: string | null;
  instructorName?: string | null;
}

export type CourseStatus =
  | { status: "DRAFT"; createdBy: string; createdAt: Temporal.Instant }
  | { status: "PUBLISHED"; publishedAt: Temporal.Instant; syllabus: string }
  | { status: "ACTIVE"; enrolledCount: number; startDate: Temporal.PlainDate }
  | { status: "ARCHIVED"; archivedAt: Temporal.Instant; finalEnrollmentCount: number }
  | { status: "CANCELLED"; reason: string; cancelledAt: Temporal.Instant };

export function describeCourse(status: CourseStatus): string {
  switch (status.status) {
    case "DRAFT":
      return `Draft created by ${status.createdBy}`;
    case "PUBLISHED":
      return `Published on ${status.publishedAt.toString()}`;
    case "ACTIVE":
      return `Active with ${status.enrolledCount} students since ${status.startDate.toString()}`;
    case "ARCHIVED":
      return `Archived with ${status.finalEnrollmentCount} completed enrollments`;
    case "CANCELLED":
      return `Cancelled: ${status.reason}`;
    default: {
      const _exhaustiveCheck: never = status;
      throw new Error(`Unhandled course status: ${JSON.stringify(_exhaustiveCheck)}`);
    }
  }
}

export interface PagedResponse<T> {
  items?: T[];
  data?: T[];
  totalCount?: number;
  page?: number;
  pageSize?: number;
  totalPages?: number;
  hasNext?: boolean;
  hasPrevious?: boolean;
  meta?: {
    totalCount: number;
    page: number;
    pageSize: number;
    totalPages: number;
    hasPrevious: boolean;
    hasNext: boolean;
  };
  links?: {
    self: string;
    next?: string | null;
    prev?: string | null;
    enroll?: string;
  };
}

export interface CourseLink {
  href: string;
  rel: string;
  method: string;
}

export interface CourseDetail extends Course {
  links: readonly CourseLink[];
}
