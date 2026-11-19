import { HttpClient } from '@angular/common/http';
import { Injectable, effect, inject, signal } from '@angular/core';
import { AuthService } from './auth.service';
import { Observable, finalize, of, shareReplay, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Product, ProductUpsertRequest } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/products`;

  // API rate limit'i sıkı (Leaky Bucket); ürün listesi sayfalar arasında önbellekten paylaşılır.
  // Ekle / güncelle / sil sonrası önbellek yenilenir.
  private readonly cache = signal<Product[] | null>(null);
  private inFlight$: Observable<Product[]> | null = null;

  readonly products = this.cache.asReadonly();

  constructor() {
    // Oturum kapanınca önbelleği bırak: sonraki kullanıcı kendi yetkisiyle yeniden yükler.
    const auth = inject(AuthService);
    effect(() => {
      if (!auth.isAuthenticated()) this.cache.set(null);
    });
  }

  // Giriş gerekir (AuthenticationBehavior); ek bir rol istenmez.
  loadAll(force = false): Observable<Product[]> {
    const cached = this.cache();
    if (cached && !force) return of(cached);
    if (force) this.inFlight$ = null;
    this.inFlight$ ??= this.http.get<Product[]>(this.base).pipe(
      tap((products) => this.cache.set(products)),
      finalize(() => (this.inFlight$ = null)),
      shareReplay(1),
    );
    return this.inFlight$;
  }

  // Product.Read / Product.Admin yetkisi gerekir.
  getById(id: string): Observable<Product> {
    return this.http.get<Product>(`${this.base}/${id}`);
  }

  create(request: ProductUpsertRequest): Observable<void> {
    return this.http.post<void>(this.base, request).pipe(tap(() => this.invalidate()));
  }

  update(id: string, request: ProductUpsertRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}`, request).pipe(tap(() => this.invalidate()));
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`).pipe(tap(() => this.invalidate()));
  }

  findCached(id: string): Product | undefined {
    return this.cache()?.find((product) => product.id === id);
  }

  private invalidate(): void {
    this.cache.set(null);
  }
}
