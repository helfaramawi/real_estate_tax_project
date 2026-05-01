import { useState } from 'react'
import { Link, useLocation, Outlet } from 'react-router-dom'
import {
  LayoutDashboard, Building2, Users, FileText, CreditCard,
  Scale, ShieldCheck, ClipboardList, TrendingUp, LogOut,
  Menu, X, ChevronLeft, Brain, BarChart3,
} from 'lucide-react'
import { logout, getStoredUser } from '../lib/auth'
import clsx from 'clsx'

const nav = [
  { to: '/', label: 'لوحة التحكم', icon: LayoutDashboard },
  { to: '/properties', label: 'العقارات', icon: Building2 },
  { to: '/taxpayers', label: 'المكلفون', icon: Users },
  { to: '/bills', label: 'فواتير الضريبة', icon: FileText },
  { to: '/payments', label: 'المدفوعات', icon: CreditCard },
  { to: '/valuations', label: 'التقييمات', icon: TrendingUp },
  { to: '/exemptions', label: 'الإعفاءات', icon: ShieldCheck },
  { to: '/appeals', label: 'الطعون', icon: Scale },
  { to: '/field-surveys', label: 'المعاينات الميدانية', icon: ClipboardList },
  { to: '/intelligence', label: 'الذكاء الاصطناعي', icon: Brain },
  { to: '/predictions', label: 'مراجعة التنبؤات', icon: BarChart3 },
]

const roleNames: Record<string, string> = {
  SuperAdmin: 'مدير النظام',
  Admin: 'مسؤول',
  Assessor: 'محاسب',
  Inspector: 'مفتش',
  Collector: 'محصل',
  Reviewer: 'مراجع',
  User: 'مستخدم',
}

export default function Layout() {
  const location = useLocation()
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const user = getStoredUser()

  return (
    <div className="flex h-screen bg-slate-50">
      {/* Mobile overlay */}
      {sidebarOpen && (
        <div
          className="fixed inset-0 bg-black/50 z-20 lg:hidden"
          onClick={() => setSidebarOpen(false)}
        />
      )}

      {/* Sidebar — fixed on right for mobile, static (RTL flex) on desktop */}
      <aside
        className={clsx(
          'fixed inset-y-0 right-0 z-30 w-64 bg-slate-900 text-white flex flex-col transition-transform duration-300',
          'lg:static lg:translate-x-0',
          sidebarOpen ? 'translate-x-0' : 'translate-x-full'
        )}
      >
        {/* Logo */}
        <div className="flex items-center justify-between px-4 py-5 border-b border-slate-700">
          <div>
            <div className="text-sm font-bold text-blue-400 tracking-wide">ريتاكس</div>
            <div className="text-xs text-slate-400 leading-tight">منصة ضريبة العقارات</div>
          </div>
          <button className="lg:hidden text-slate-400 hover:text-white" onClick={() => setSidebarOpen(false)}>
            <X size={18} />
          </button>
        </div>

        {/* Navigation */}
        <nav className="flex-1 overflow-y-auto py-4 px-2">
          {nav.map(({ to, label, icon: Icon }) => {
            const active = to === '/'
              ? location.pathname === '/'
              : location.pathname.startsWith(to)
            return (
              <Link
                key={to}
                to={to}
                onClick={() => setSidebarOpen(false)}
                className={clsx(
                  'flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium mb-0.5 transition-colors',
                  active
                    ? 'bg-blue-600 text-white'
                    : 'text-slate-300 hover:bg-slate-800 hover:text-white'
                )}
              >
                <Icon size={18} />
                {label}
                {active && <ChevronLeft size={14} className="ms-auto" />}
              </Link>
            )
          })}
        </nav>

        {/* User footer */}
        <div className="border-t border-slate-700 px-4 py-3">
          <div className="text-sm text-slate-300 font-medium truncate">{user?.username}</div>
          <div className="text-xs text-slate-500 mb-2">
            {roleNames[user?.roles?.[0] ?? ''] ?? user?.roles?.[0] ?? 'مستخدم'}
          </div>
          <button
            onClick={logout}
            className="flex items-center gap-2 text-xs text-slate-400 hover:text-red-400 transition-colors"
          >
            <LogOut size={14} /> تسجيل الخروج
          </button>
        </div>
      </aside>

      {/* Main content */}
      <div className="flex-1 flex flex-col overflow-hidden min-w-0">
        {/* Top bar */}
        <header className="bg-white border-b border-slate-200 px-4 py-3 flex items-center gap-3">
          <button
            className="lg:hidden text-slate-500 hover:text-slate-700"
            onClick={() => setSidebarOpen(true)}
          >
            <Menu size={20} />
          </button>
          <div className="flex-1" />
          <div className="text-xs text-slate-400">
            نظام إدارة الضريبة على العقارات — جمهورية مصر العربية
          </div>
        </header>

        {/* Page content */}
        <main className="flex-1 overflow-y-auto p-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
