export interface UserAccount {
  id: string;
  email: string;
  userName: string;
  firstName: string;
  lastName: string;
  displayName: string;
  role: string;
  roles?: string[];
  isApproved: boolean;
  approvalStatus: 'Pending' | 'Approved' | 'Rejected';
  createdAt: string;
  lastLoginAt?: string;
}
