import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { UserAccount } from '../models/user-account.model';

@Injectable({
  providedIn: 'root',
})
export class AdminUserService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/auth/admin/users`;

  getUsers(): Observable<UserAccount[]> {
    return this.http.get<UserAccount[]>(this.baseUrl);
  }

  approveUser(id: string): Observable<{ message: string; userId: string; isApproved: boolean; approvalStatus: string }> {
    return this.http.post<{ message: string; userId: string; isApproved: boolean; approvalStatus: string }>(
      `${this.baseUrl}/${id}/approve`,
      {}
    );
  }

  rejectUser(id: string): Observable<{ message: string; userId: string; isApproved: boolean; approvalStatus: string }> {
    return this.http.post<{ message: string; userId: string; isApproved: boolean; approvalStatus: string }>(
      `${this.baseUrl}/${id}/reject`,
      {}
    );
  }

  deleteUser(id: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.baseUrl}/${id}`);
  }
}
