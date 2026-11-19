import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateOrderItemRequest,
  CreateOrderRequest,
  Order,
  OrderItem,
  UpdateOrderItemRequest,
  UpdateOrderRequest,
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);
  private readonly orders = `${environment.apiUrl}/orders`;
  private readonly orderItems = `${environment.apiUrl}/orderitems`;

  // ---------- Orders ----------
  getAll(): Observable<Order[]> {
    return this.http.get<Order[]>(this.orders);
  }

  getById(id: string): Observable<Order> {
    return this.http.get<Order>(`${this.orders}/${id}`);
  }

  create(request: CreateOrderRequest): Observable<void> {
    return this.http.post<void>(this.orders, request);
  }

  update(id: string, request: UpdateOrderRequest): Observable<void> {
    return this.http.put<void>(`${this.orders}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.orders}/${id}`);
  }

  // ---------- Order items ----------
  // API sipariş bazlı filtre sunmuyor; tüm kalemler alınıp istemcide süzülür.
  getAllItems(): Observable<OrderItem[]> {
    return this.http.get<OrderItem[]>(this.orderItems);
  }

  addItem(request: CreateOrderItemRequest): Observable<void> {
    return this.http.post<void>(this.orderItems, request);
  }

  updateItem(id: string, request: UpdateOrderItemRequest): Observable<void> {
    return this.http.put<void>(`${this.orderItems}/${id}`, request);
  }

  deleteItem(id: string): Observable<void> {
    return this.http.delete<void>(`${this.orderItems}/${id}`);
  }
}
