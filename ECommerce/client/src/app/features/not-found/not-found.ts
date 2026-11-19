import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Icon } from '../../shared/components/icon';

@Component({
  selector: 'app-not-found',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, Icon],
  template: `
    <div class="container page">
      <div class="card empty" style="padding: 80px 24px">
        <div class="empty__icon"><app-icon name="mountain" [size]="26" /></div>
        <span class="eyebrow">404</span>
        <h1>Bu patika bir yere çıkmıyor</h1>
        <p>Aradığınız sayfa bulunamadı ya da taşınmış olabilir.</p>
        <a routerLink="/" class="btn btn--primary">Ana sayfaya dön</a>
      </div>
    </div>
  `,
})
export class NotFound {}
