import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'contacts' },
  {
    path: 'contacts',
    title: 'Contacts',
    loadComponent: () =>
      import('./contacts/feature/contact-list.component').then((m) => m.ContactListComponent),
  },
  {
    path: 'contacts/new',
    title: 'New contact',
    loadComponent: () =>
      import('./contacts/feature/contact-form.component').then((m) => m.ContactFormComponent),
  },
  {
    path: 'contacts/:id/edit',
    title: 'Edit contact',
    loadComponent: () =>
      import('./contacts/feature/contact-form.component').then((m) => m.ContactFormComponent),
  },
  { path: '**', redirectTo: 'contacts' },
];
