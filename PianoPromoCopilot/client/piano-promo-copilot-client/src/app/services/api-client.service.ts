import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';

/**
 * Normalised API failure. Keeps the HTTP status and the server-provided body so
 * components can branch on `status` (0 = never reached the server, 404, ...),
 * while `message` stays a human-readable string for the simpler `err.message`
 * error handlers.
 */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    message: string,
    readonly error: any
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

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

    // status 0 = the request never reached the server (backend down, CORS, DNS).
    // With withFetch() the raw body is then a TypeError like "Failed to fetch",
    // which is noise - drop it so callers fall back to the reachability hint.
    const body = error.status === 0 ? null : error.error;
    const serverMessage = typeof body === 'string' ? body : body?.message;

    const message =
      serverMessage ||
      (error.status === 0
        ? `Cannot reach the API at ${environment.apiUrl}. Is the backend running?`
        : `Request failed (HTTP ${error.status}).`);

    return throwError(() => new ApiError(error.status, message, body));
  }
}
