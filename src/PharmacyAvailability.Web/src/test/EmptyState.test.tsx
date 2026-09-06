import { describe, it, expect, vi } from 'vitest';
import { screen, fireEvent, render } from '@testing-library/react';
import { EmptyState } from '@/components/feedback/EmptyState';
import { PackageOpen } from 'lucide-react';

describe('EmptyState', () => {
  it('renders title and description', () => {
    render(
      <EmptyState
        title="No Results Found"
        description="Try searching for something else."
      />
    );

    expect(screen.getByText('No Results Found')).toBeInTheDocument();
    expect(screen.getByText('Try searching for something else.')).toBeInTheDocument();
  });

  it('renders action button and triggers callback when provided', () => {
    const handleAction = vi.fn();
    render(
      <EmptyState
        icon={PackageOpen}
        title="Empty Table"
        description="Nothing to display"
        actionLabel="Create Item"
        onAction={handleAction}
      />
    );

    const button = screen.getByRole('button', { name: 'Create Item' });
    expect(button).toBeInTheDocument();

    fireEvent.click(button);
    expect(handleAction).toHaveBeenCalledTimes(1);
  });
});
