export function Footer() {
  return (
    <footer className="mt-auto border-t border-slate-200 bg-white py-6">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex flex-col sm:flex-row items-center justify-between gap-4 text-xs text-slate-500">
          <p>© {new Date().getFullYear()} Pharmacy Medicine Availability Platform. All rights reserved.</p>
          <div className="flex items-center space-x-6">
            <span>Real-Time Inventory</span>
            <span>•</span>
            <span>Branch Operations</span>
            <span>•</span>
            <span>Audited Reservations</span>
          </div>
        </div>
      </div>
    </footer>
  );
}
