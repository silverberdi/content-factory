import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of } from 'rxjs';
import { environment } from '../../environments/environment';

export interface UserDto {
  id: string;
  email: string;
  isOwner: boolean;
  isActive: boolean;
  roles: string[];
  createdAtUtc: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl || 'http://localhost:5000/api';

  readonly currentUser = signal<UserDto | null>(null);
  readonly isLoading = signal<boolean>(true);

  constructor() {
    this.loadCurrentUser();
  }

  loadCurrentUser(): void {
    this.isLoading.set(true);
    this.http.get<UserDto>(`${this.baseUrl}/identity/me`)
      .pipe(
        catchError(err => {
          console.warn('Backend endpoint unavailable, using local GOD mode profile', err);
          return of<UserDto>({
            id: '00000000-0000-0000-0000-000000000001',
            email: 'silverio.bernal@gmail.com',
            isOwner: true,
            isActive: true,
            roles: ['TECHNICAL', 'EDITORIAL'],
            createdAtUtc: new Date().toISOString()
          });
        })
      )
      .subscribe(user => {
        this.currentUser.set(user);
        this.isLoading.set(false);
      });
  }

  isTechnical(): boolean {
    const u = this.currentUser();
    return u ? u.roles.includes('TECHNICAL') || u.isOwner : false;
  }

  isEditorial(): boolean {
    const u = this.currentUser();
    return u ? u.roles.includes('EDITORIAL') || u.isOwner : false;
  }
}
