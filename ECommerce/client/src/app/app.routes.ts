import { Routes } from '@angular/router';
import { authGuard, guestGuard, mfaGuard } from './core/guards/auth.guards';

export const routes: Routes = [
  {
    path: '',
    title: 'Uludağ Market',
    loadComponent: () => import('./features/home/home').then((m) => m.Home),
  },
  {
    path: 'products/:id',
    title: 'Ürün detayı · Uludağ Market',
    canActivate: [authGuard],
    loadComponent: () => import('./features/product-detail/product-detail').then((m) => m.ProductDetail),
  },
  {
    path: 'cart',
    title: 'Sepet · Uludağ Market',
    loadComponent: () => import('./features/cart/cart').then((m) => m.Cart),
  },

  // ---------- Kimlik ----------
  {
    path: 'login',
    title: 'Giriş · Uludağ Market',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login').then((m) => m.Login),
  },
  {
    path: 'register',
    title: 'Kayıt · Uludağ Market',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/register').then((m) => m.Register),
  },
  {
    path: 'mfa/setup',
    title: 'Authenticator kurulumu · Uludağ Market',
    canActivate: [mfaGuard('SetupRequired')],
    loadComponent: () => import('./features/auth/mfa-setup').then((m) => m.MfaSetup),
  },
  {
    path: 'mfa/verify',
    title: 'Doğrulama kodu · Uludağ Market',
    canActivate: [mfaGuard('CodeRequired')],
    loadComponent: () => import('./features/auth/mfa-verify').then((m) => m.MfaVerify),
  },
  {
    path: 'account',
    title: 'Hesabım · Uludağ Market',
    canActivate: [authGuard],
    loadComponent: () => import('./features/account/account').then((m) => m.Account),
  },

  // ---------- Siparişler ----------
  {
    path: 'orders',
    title: 'Siparişler · Uludağ Market',
    canActivate: [authGuard],
    loadComponent: () => import('./features/orders/order-list').then((m) => m.OrderList),
  },
  {
    path: 'orders/:id',
    title: 'Sipariş detayı · Uludağ Market',
    canActivate: [authGuard],
    loadComponent: () => import('./features/orders/order-detail').then((m) => m.OrderDetail),
  },

  // ---------- Yönetim ----------
  {
    path: 'admin/products',
    title: 'Ürün yönetimi · Uludağ Market',
    canActivate: [authGuard],
    loadComponent: () => import('./features/admin/admin-products').then((m) => m.AdminProducts),
  },
  {
    path: 'admin/claims',
    title: 'Yetki atama · Uludağ Market',
    canActivate: [authGuard],
    loadComponent: () => import('./features/admin/admin-claims').then((m) => m.AdminClaims),
  },

  {
    path: '**',
    title: 'Sayfa bulunamadı · Uludağ Market',
    loadComponent: () => import('./features/not-found/not-found').then((m) => m.NotFound),
  },
];
