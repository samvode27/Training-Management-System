import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface StudentGoal {
  id: number;
  studentId: string;
  title: string;
  targetTerm: string;
  targetCredits: number;
  isCompleted: boolean;
  createdAt: string;
}

export interface CreateStudentGoalPayload {
  studentId: string;
  title: string;
  targetTerm: string;
  targetCredits: number;
}

export interface UpdateStudentGoalPayload {
  isCompleted?: boolean;
  title?: string;
  targetTerm?: string;
  targetCredits?: number;
}

@Injectable({
  providedIn: 'root',
})
export class StudentGoalService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/student-goals`;

  getGoals(studentId?: string) {
    const params: Record<string, string> = {};
    if (studentId) {
      params['studentId'] = studentId;
    }
    return this.http.get<StudentGoal[]>(this.baseUrl, { params });
  }

  createGoal(payload: CreateStudentGoalPayload) {
    return this.http.post<StudentGoal>(this.baseUrl, payload);
  }

  updateGoal(id: number, payload: UpdateStudentGoalPayload) {
    return this.http.put<StudentGoal>(`${this.baseUrl}/${id}`, payload);
  }

  deleteGoal(id: number) {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
