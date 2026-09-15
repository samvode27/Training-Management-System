import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface GradePayload {
  studentId: number;
  courseId: number;
  score: number;
}

@Injectable({ providedIn: 'root' })
export class GradeService {
  private http = inject(HttpClient);

  postGrade(payload: GradePayload): Observable<{ id: string; success: boolean }> {
    return this.http.post<{ id: string; success: boolean }>(`${environment.apiUrl}/grades`, payload);
  }
}
