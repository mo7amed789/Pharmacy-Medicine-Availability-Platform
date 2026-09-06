import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { SkeletonCard } from '@/components/feedback/SkeletonCard';

describe('SkeletonCard', () => {
  it('renders default 3 skeleton cards with pulse elements', () => {
    const { container } = render(<SkeletonCard />);
    const cards = container.querySelectorAll('.animate-pulse');
    expect(cards.length).toBe(3);
    expect(screen.getAllByRole('status').length).toBe(3);
  });

  it('renders requested number of skeleton cards', () => {
    render(<SkeletonCard count={4} />);
    expect(screen.getAllByRole('status').length).toBe(4);
  });
});
