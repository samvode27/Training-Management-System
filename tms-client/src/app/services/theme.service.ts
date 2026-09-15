import { Injectable, signal, effect } from '@angular/core';

export type ThemeMode = 'light' | 'dark';

@Injectable({
  providedIn: 'root',
})
export class ThemeService {
  private readonly THEME_KEY = 'tms_theme_preference';
  
  isDark = signal<boolean>(this.getInitialTheme() === 'dark');

  constructor() {
    effect(() => {
      const dark = this.isDark();
      const theme: ThemeMode = dark ? 'dark' : 'light';
      document.documentElement.setAttribute('data-theme', theme);
      if (dark) {
        document.body.classList.add('dark-theme');
        document.body.classList.remove('light-theme');
      } else {
        document.body.classList.add('light-theme');
        document.body.classList.remove('dark-theme');
      }
      try {
        localStorage.setItem(this.THEME_KEY, theme);
      } catch (e) {
        console.warn('Could not save theme preference:', e);
      }
    });
  }

  private getInitialTheme(): ThemeMode {
    try {
      const saved = localStorage.getItem(this.THEME_KEY);
      if (saved === 'dark' || saved === 'light') {
        return saved;
      }
      if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
        return 'dark';
      }
    } catch (e) {
      console.warn('Error reading theme preference:', e);
    }
    return 'light';
  }

  toggleTheme(): void {
    this.isDark.update((v) => !v);
  }
}
