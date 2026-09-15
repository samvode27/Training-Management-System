import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AdminUserService } from '../../services/admin-user.service';
import { UserAccount } from '../../models/user-account.model';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatButtonModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-users.component.scss',
})
export class AdminUsersComponent implements OnInit {
  private api = inject(AdminUserService);

  users = signal<UserAccount[]>([]);
  isLoading = signal(false);
  filterTab = signal<'pending' | 'all' | 'instructor' | 'student' | 'rejected'>('pending');
  searchTerm = signal('');
  actionInProgressId = signal<string | null>(null);
  feedbackMessage = signal<string | null>(null);

  displayedColumns: string[] = [
    'user',
    'email',
    'role',
    'registeredAt',
    'status',
    'actions',
  ];

  // Computed metrics
  pendingCount = computed(() => this.users().filter((u) => u.approvalStatus === 'Pending').length);
  approvedCount = computed(() => this.users().filter((u) => u.approvalStatus === 'Approved').length);
  rejectedCount = computed(() => this.users().filter((u) => u.approvalStatus === 'Rejected').length);
  totalCount = computed(() => this.users().length);

  // Filtered view
  filteredUsers = computed(() => {
    let list = this.users();
    const tab = this.filterTab();
    const search = this.searchTerm().toLowerCase().trim();

    if (tab === 'pending') {
      list = list.filter((u) => u.approvalStatus === 'Pending');
    } else if (tab === 'instructor') {
      list = list.filter((u) => u.role === 'Instructor');
    } else if (tab === 'student') {
      list = list.filter((u) => u.role === 'Student');
    } else if (tab === 'rejected') {
      list = list.filter((u) => u.approvalStatus === 'Rejected');
    }

    if (search) {
      list = list.filter(
        (u) =>
          u.displayName.toLowerCase().includes(search) ||
          u.email.toLowerCase().includes(search) ||
          u.role.toLowerCase().includes(search)
      );
    }

    return list;
  });

  ngOnInit() {
    this.loadUsers();
  }

  loadUsers() {
    this.isLoading.set(true);
    this.api.getUsers().subscribe({
      next: (data) => {
        this.users.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      },
    });
  }

  approve(user: UserAccount) {
    this.actionInProgressId.set(user.id);
    this.api.approveUser(user.id).subscribe({
      next: () => {
        this.users.update((list) =>
          list.map((u) =>
            u.id === user.id ? { ...u, isApproved: true, approvalStatus: 'Approved' } : u
          )
        );
        this.actionInProgressId.set(null);
        this.showFeedback(`✅ Account for ${user.displayName || user.email} has been Approved & Activated.`);
      },
      error: () => {
        this.actionInProgressId.set(null);
        this.showFeedback(`❌ Failed to approve account.`);
      },
    });
  }

  reject(user: UserAccount) {
    this.actionInProgressId.set(user.id);
    this.api.rejectUser(user.id).subscribe({
      next: () => {
        this.users.update((list) =>
          list.map((u) =>
            u.id === user.id ? { ...u, isApproved: false, approvalStatus: 'Rejected' } : u
          )
        );
        this.actionInProgressId.set(null);
        this.showFeedback(`⚠️ Registration request for ${user.displayName || user.email} was Declined.`);
      },
      error: () => {
        this.actionInProgressId.set(null);
        this.showFeedback(`❌ Failed to reject account.`);
      },
    });
  }

  showFeedback(msg: string) {
    this.feedbackMessage.set(msg);
    setTimeout(() => {
      this.feedbackMessage.set(null);
    }, 4000);
  }
}
