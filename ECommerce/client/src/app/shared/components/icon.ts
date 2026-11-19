import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

// Küçük, bağımlılıksız ikon seti (24x24, stroke tabanlı).
const ICONS: Record<string, string> = {
  cart: 'M3 4h2l2.4 11.2a2 2 0 0 0 2 1.6h7.7a2 2 0 0 0 2-1.5L21 8H6.2M10 20.5a.5.5 0 1 1-1 0 .5.5 0 0 1 1 0Zm8 0a.5.5 0 1 1-1 0 .5.5 0 0 1 1 0Z',
  user: 'M20 21a8 8 0 0 0-16 0M12 13a5 5 0 1 0 0-10 5 5 0 0 0 0 10Z',
  shield: 'M12 22s8-3.5 8-10V5l-8-3-8 3v7c0 6.5 8 10 8 10Zm-3-10 2 2 4-4',
  box: 'M21 8 12 3 3 8m18 0v8l-9 5m9-13-9 5m0 8-9-5V8m9 13v-8M3 8l9 5',
  receipt: 'M6 2h12v20l-3-2-3 2-3-2-3 2V2Zm3 6h6m-6 4h6m-6 4h3',
  plus: 'M12 5v14M5 12h14',
  minus: 'M5 12h14',
  trash: 'M4 7h16M10 11v6m4-6v6M6 7l1 13h10l1-13M9 7V4h6v3',
  edit: 'M4 20h4L19 9l-4-4L4 16v4Zm9-13 4 4',
  search: 'm21 21-4.3-4.3M11 18a7 7 0 1 1 0-14 7 7 0 0 1 0 14Z',
  logout: 'M15 4h3a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2h-3M10 17l5-5-5-5m5 5H3',
  key: 'M15 7a3 3 0 1 1 0 6 3 3 0 0 1 0-6Zm-2.1 5.1L4 21m3-3 2 2m-1-4 2 2',
  copy: 'M9 9h11v11H9V9Zm-4 6H4V4h11v1',
  check: 'm5 12 5 5L20 7',
  x: 'M6 6l12 12M18 6 6 18',
  arrowLeft: 'M19 12H5m6-6-6 6 6 6',
  arrowRight: 'M5 12h14m-6-6 6 6-6 6',
  refresh: 'M20 11a8 8 0 0 0-14.9-4M4 4v4h4m-4 5a8 8 0 0 0 14.9 4M20 20v-4h-4',
  mountain: 'm3 20 6.5-11 4 6 2.5-4L21 20H3Zm6.5-11 1.5 2.5',
  info: 'M12 16v-4m0-4h.01M12 22a10 10 0 1 1 0-20 10 10 0 0 1 0 20Z',
  lock: 'M6 11h12v10H6V11Zm2 0V7a4 4 0 0 1 8 0v4',
  smartphone: 'M7 2h10v20H7V2Zm4 17h2',
  menu: 'M4 6h16M4 12h16M4 18h16',
};

@Component({
  selector: 'app-icon',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'icon', 'aria-hidden': 'true' },
  template: `
    <svg [attr.width]="size()" [attr.height]="size()" viewBox="0 0 24 24" fill="none"
         stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
      <path [attr.d]="path()" />
    </svg>
  `,
  styles: `:host { display: inline-flex; line-height: 0; }`,
})
export class Icon {
  readonly name = input.required<string>();
  readonly size = input(20);
  protected readonly path = computed(() => ICONS[this.name()] ?? '');
}
