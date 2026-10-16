import { PanelLeftClose, PanelLeftOpen } from 'lucide-react'
import { NavLink } from 'react-router'
import { cn } from '../../lib/cn'
import { useAuth } from '../../features/auth/AuthContext'
import { Logo } from '../ui/Logo'
import { navigationFor } from './navigation'

/** Barra lateral oscura (estilo CRM). collapsed = solo iconos con tooltip nativo (title). */
export function Sidebar({ collapsed, onToggleCollapsed, onNavigate, mobile = false }: {
  collapsed: boolean
  onToggleCollapsed?: () => void
  onNavigate?: () => void
  mobile?: boolean
}) {
  const { user } = useAuth()
  if (!user) return null
  const groups = navigationFor(user.role)
  const compact = collapsed && !mobile

  return (
    <div className="flex h-full flex-col bg-sidebar text-sidebar-fg">
      <div className={cn('flex h-16 items-center border-b border-white/5', compact ? 'justify-center px-2' : 'px-5')}>
        <Logo withText={!compact} textClassName="text-white" />
      </div>

      <nav aria-label="Principal" className="scrollbar-thin flex-1 space-y-6 overflow-y-auto px-3 py-5">
        {groups.map((group) => (
          <div key={group.label}>
            {!compact && <p className="mb-2 px-2.5 text-[11px] font-semibold tracking-wider text-sidebar-muted uppercase">{group.label}</p>}
            <ul className="space-y-0.5">
              {group.items.map((item) => (
                <li key={item.to}>
                  <NavLink
                    to={item.to}
                    end={item.to === '/sales'}
                    onClick={onNavigate}
                    title={compact ? item.label : undefined}
                    className={({ isActive }) => cn(
                      'group relative flex items-center gap-3 rounded-lg px-2.5 py-2 text-sm font-medium transition-colors [&_svg]:size-[18px] [&_svg]:shrink-0',
                      compact && 'justify-center',
                      isActive ? 'bg-sidebar-active text-white' : 'text-sidebar-fg/80 hover:bg-white/5 hover:text-white',
                    )}
                  >
                    {({ isActive }) => (
                      <>
                        {isActive && <span className="absolute inset-y-1.5 left-0 w-0.5 rounded-full bg-indigo-400" aria-hidden />}
                        <span className={isActive ? 'text-indigo-300' : 'text-sidebar-muted group-hover:text-sidebar-fg'}>{item.icon}</span>
                        {!compact && <span className="truncate">{item.label}</span>}
                      </>
                    )}
                  </NavLink>
                </li>
              ))}
            </ul>
          </div>
        ))}
      </nav>

      {!mobile && onToggleCollapsed && (
        <div className="border-t border-white/5 p-3">
          <button
            type="button"
            onClick={onToggleCollapsed}
            className={cn('flex w-full items-center gap-3 rounded-lg px-2.5 py-2 text-sm text-sidebar-muted hover:bg-white/5 hover:text-white', compact && 'justify-center')}
            aria-label={compact ? 'Expandir menú' : 'Contraer menú'}
          >
            {compact ? <PanelLeftOpen className="size-[18px]" /> : <><PanelLeftClose className="size-[18px]" /> Contraer</>}
          </button>
        </div>
      )}
    </div>
  )
}
