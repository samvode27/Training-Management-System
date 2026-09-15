import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { Course, CourseDetail, PagedResponse } from '../models/course.model';

export interface CreateCoursePayload {
  code: string;
  title: string;
  maxCapacity: number;
  instructorId?: string | number | null;
  department?: string;
  credits?: number;
  summary?: string;
  description?: string;
  prerequisites?: string;
  learningOutcomesJson?: string;
  syllabusJson?: string;
  industrySkillsJson?: string;
}

@Injectable({
  providedIn: 'root',
})
export class CourseService {
  private http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/courses`;

  getAll() {
    return this.http
      .get<PagedResponse<Course>>(this.base, {
        params: { page: '1', pageSize: '50' },
      })
      .pipe(map((response) => response.items ?? (response as any).data ?? []));
  }

  getById(id: string | number) {
    return this.http.get<CourseDetail>(`/api/courses/${id}`).pipe(
      catchError(() =>
        this.getAll().pipe(
          map((courses) => {
            const match = courses.find((c: Course) => String(c.id) === String(id));
            if (!match) throw new Error('Course not found');
            return match as CourseDetail;
          })
        )
      )
    );
  }

  create(payload: CreateCoursePayload) {
    return this.http.post<Course>(this.base, payload);
  }

  createCourse(payload: CreateCoursePayload) {
    return this.create(payload);
  }

  update(id: number | string, payload: Partial<CreateCoursePayload>) {
    return this.http.put<Course>(`${this.base}/${id}`, payload);
  }

  delete(id: number | string) {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  deleteCourse(id: number | string) {
    return this.delete(id);
  }
}
