import { Component, inject, OnInit, signal } from '@angular/core';
import { Router, RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { EnrollmentStore } from './store/enrollment.store';
import { AuthService } from './services/auth.service';
import { ThemeService } from './services/theme.service';
import { LiveSyncService } from './services/live-sync.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  protected readonly title = signal('tms-client');
  private store = inject(EnrollmentStore);
  public auth = inject(AuthService);
  public themeService = inject(ThemeService);
  public liveSync = inject(LiveSyncService);
  private router = inject(Router);

  async ngOnInit() {
    await this.auth.checkSession();
    this.store.listenForLiveUpdates();
  }

  async logout() {
    await this.auth.logout();
    this.router.navigate(['/login']);
  }
}
