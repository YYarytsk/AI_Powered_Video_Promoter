import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ApiClientService {
  readonly baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  get<T>(path: string): Observable<T> {
    return this.http.get<T>(`${this.baseUrl}/${path}`).pipe(catchError(this.handleError));
  }

  post<T>(path: string, body: any): Observable<T> {
    return this.http.post<T>(`${this.baseUrl}/${path}`, body).pipe(catchError(this.handleError));
  }

  put<T>(path: string, body: any): Observable<T> {
    return this.http.put<T>(`${this.baseUrl}/${path}`, body).pipe(catchError(this.handleError));
  }

  private handleError(error: HttpErrorResponse): Observable<never> {
    console.error('API Error:', error);
    const message = error.error?.message || error.message || 'An unknown error occurred';
    return throwError(() => new Error(message));
  }
}
