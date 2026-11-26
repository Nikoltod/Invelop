import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { Contact, CreateContactRequest } from './contact.model';

/**
 * The only place in the application that knows a URL.
 *
 * Deliberately thin: no error handling, no state, no mapping. Effects decide what a failure
 * means; this just makes the request.
 */
@Injectable({ providedIn: 'root' })
export class ContactsApiService {
  private readonly http = inject(HttpClient);

  // Relative, so the dev server's proxy forwards it to the API in development and the same
  // build works unchanged if the SPA is ever served from the API's own origin.
  private readonly baseUrl = '/api/contacts';

  getAll(): Observable<Contact[]> {
    return this.http.get<Contact[]>(this.baseUrl);
  }

  get(id: string): Observable<Contact> {
    return this.http.get<Contact>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateContactRequest): Observable<Contact> {
    return this.http.post<Contact>(this.baseUrl, request);
  }

  update(id: string, request: CreateContactRequest): Observable<Contact> {
    return this.http.put<Contact>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
