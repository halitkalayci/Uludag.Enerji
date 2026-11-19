import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

// Ürünlerin görseli yok: adından türetilen, her zaman aynı kalan bir "dağ manzarası" kartı çizer.
const PALETTES: [string, string, string][] = [
  ['#e7f0ea', '#9cc5ad', '#2f6b52'],
  ['#fdf0e1', '#f2c08d', '#b5652a'],
  ['#e8eef8', '#a7bde3', '#3a5a94'],
  ['#f6e9ee', '#e0a9bb', '#94405d'],
  ['#efedf8', '#bdb4e6', '#5a4a9e'],
  ['#e6f3f3', '#98cfcf', '#2a7272'],
  ['#f5f1e3', '#d9c98f', '#7d6a1f'],
];

function hash(text: string): number {
  let value = 2166136261;
  for (let i = 0; i < text.length; i++) {
    value ^= text.charCodeAt(i);
    value = Math.imul(value, 16777619);
  }
  return value >>> 0;
}

@Component({
  selector: 'app-product-art',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <svg viewBox="0 0 320 200" preserveAspectRatio="xMidYMid slice" role="img" [attr.aria-label]="name()">
      <rect width="320" height="200" [attr.fill]="palette()[0]" />
      <circle [attr.cx]="sunX()" cy="58" r="22" fill="#fff" opacity=".85" />
      <path [attr.d]="backRidge()" [attr.fill]="palette()[1]" />
      <path [attr.d]="frontRidge()" [attr.fill]="palette()[2]" />
      @if (!compact()) {
        <text x="24" y="178" class="initials">{{ initials() }}</text>
      }
    </svg>
  `,
  styles: `
    :host { display: block; overflow: hidden; }
    svg { display: block; width: 100%; height: 100%; }
    .initials { font: 600 30px var(--font-display); fill: #fff; letter-spacing: .02em; }
  `,
})
export class ProductArt {
  readonly name = input<string | null>('');
  // Küçük önizlemelerde (tablo, sepet) baş harfler kırpılır; gizlenir.
  readonly compact = input(false);

  private readonly seed = computed(() => hash(this.name() ?? '?'));
  protected readonly palette = computed(() => PALETTES[this.seed() % PALETTES.length]);
  protected readonly sunX = computed(() => 200 + (this.seed() % 90));

  protected readonly backRidge = computed(() => {
    const s = this.seed();
    const a = 60 + (s % 50);
    const b = 150 + ((s >> 4) % 60);
    return `M0 200 L0 ${a + 40} L${b - 70} ${a} L${b} ${a + 50} L${b + 60} ${a - 10} L320 ${a + 60} L320 200 Z`;
  });

  protected readonly frontRidge = computed(() => {
    const s = this.seed() >> 8;
    const peak = 90 + (s % 40);
    const x = 80 + ((s >> 3) % 120);
    return `M0 200 L0 160 L${x} ${peak} L${x + 40} ${peak + 25} L${x + 90} ${peak + 5} L320 170 L320 200 Z`;
  });

  protected readonly initials = computed(() =>
    (this.name() ?? '?')
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((word) => word[0]!.toLocaleUpperCase('tr-TR'))
      .join(''),
  );
}
