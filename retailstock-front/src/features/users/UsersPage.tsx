import { Ban, KeyRound, Pencil, RotateCcw, ShieldCheck, ShoppingCart, Sparkles, UserPlus, Users, Warehouse } from 'lucide-react'
import { omit } from '../../lib/omit'
import { useState, type FormEvent, type ReactNode } from 'react'
import { toast } from 'sonner'
import { getErrorMessage, getFieldErrors, toastError } from '../../api/errors'
import type { Role } from '../../api/types'
import { Alert } from '../../components/ui/Alert'
import { Avatar } from '../../components/ui/Avatar'
import { Badge, type BadgeTone } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { ConfirmDialog } from '../../components/ui/ConfirmDialog'
import { DataTable, type Column } from '../../components/ui/DataTable'
import { Dialog } from '../../components/ui/Dialog'
import { Drawer } from '../../components/ui/Drawer'
import { DropdownMenu } from '../../components/ui/DropdownMenu'
import { EmptyState } from '../../components/ui/EmptyState'
import { Field, Input, Select } from '../../components/ui/Field'
import { PageHeader } from '../../components/ui/PageHeader'
import { Pagination } from '../../components/ui/Pagination'
import { SearchInput } from '../../components/ui/SearchInput'
import { useApiQuery } from '../../hooks/useApiQuery'
import { useDebounce } from '../../hooks/useDebounce'
import { cn } from '../../lib/cn'
import { useAuth } from '../auth/AuthContext'
import { roleLabels } from '../auth/roles'
import { usersApi, type User } from './usersApi'

const PAGE_SIZE = 10

const roleMeta: Record<Role, { tone: BadgeTone; icon: ReactNode; description: string }> = {
  Admin: { tone: 'primary', icon: <ShieldCheck />, description: 'Acceso total: catálogo, reportes, usuarios y SAP.' },
  Cashier: { tone: 'info', icon: <ShoppingCart />, description: 'Registra ventas y consulta productos y existencias.' },
  Warehouse: { tone: 'warning', icon: <Warehouse />, description: 'Ingresos, egresos, transferencias y kardex.' },
}

export function UsersPage() {
  const { user: me } = useAuth()
  const [search, setSearch] = useState('')
  const [role, setRole] = useState<Role | ''>('')
  const [page, setPage] = useState(1)
  const [creating, setCreating] = useState(false)
  const [editing, setEditing] = useState<User | null>(null)
  const [resetting, setResetting] = useState<User | null>(null)
  const [toggling, setToggling] = useState<User | null>(null)
  const debounced = useDebounce(search.trim())

  const list = useApiQuery(
    (signal) => usersApi.list({ page, pageSize: PAGE_SIZE, name: debounced || undefined, role: role || undefined }, signal),
    [page, debounced, role],
  )
  const isMe = (u: User) => u.email === me?.email

  async function handleToggle() {
    if (!toggling) return
    try {
      await usersApi.update(toggling.id, { role: toggling.role, isActive: !toggling.isActive })
      toast.success(toggling.isActive ? `${toggling.email} desactivado` : `${toggling.email} reactivado`)
      list.reload()
    } catch (err) {
      toastError(err)
      throw err
    }
  }

  const columns: Column<User>[] = [
    {
      key: 'user', header: 'Usuario',
      cell: (u) => (
        <span className="flex items-center gap-3">
          <Avatar name={u.email} />
          <span className="min-w-0">
            <span className="flex items-center gap-2 font-medium">
              <span className="truncate">{u.email}</span>
              {isMe(u) && <Badge tone="primary">Tú</Badge>}
            </span>
            <span className="block text-xs text-fg-muted">ID #{u.id}</span>
          </span>
        </span>
      ),
    },
    {
      key: 'role', header: 'Rol',
      cell: (u) => <Badge tone={roleMeta[u.role].tone} icon={<span className="[&_svg]:size-3">{roleMeta[u.role].icon}</span>}>{roleLabels[u.role]}</Badge>,
    },
    { key: 'status', header: 'Estado', cell: (u) => <Badge tone={u.isActive ? 'success' : 'neutral'} dot>{u.isActive ? 'Activo' : 'Desactivado'}</Badge>, hideBelow: 'sm' },
    {
      key: 'actions', header: <span className="sr-only">Acciones</span>, align: 'right', className: 'w-12',
      cell: (u) => (
        <DropdownMenu items={[
          { label: 'Editar rol', icon: <Pencil />, onSelect: () => setEditing(u), disabled: isMe(u) },
          { label: 'Restablecer contraseña', icon: <KeyRound />, onSelect: () => setResetting(u) },
          u.isActive
            ? { label: 'Desactivar', icon: <Ban />, tone: 'danger', onSelect: () => setToggling(u), disabled: isMe(u) }
            : { label: 'Reactivar', icon: <RotateCcw />, onSelect: () => setToggling(u) },
        ]} />
      ),
    },
  ]

  return (
    <>
      <PageHeader
        title="Usuarios"
        description="Cuentas del personal y sus permisos."
        actions={<Button icon={<UserPlus className="size-4" />} onClick={() => setCreating(true)}>Nuevo usuario</Button>}
      />

      <div className="mb-4 flex flex-wrap items-center gap-3">
        <SearchInput value={search} onChange={(v) => { setSearch(v); setPage(1) }} placeholder="Buscar por email…" className="w-full sm:w-72" />
        <div className="w-full sm:w-48">
          <Select value={role} onChange={(e) => { setRole(e.target.value as Role | ''); setPage(1) }} aria-label="Filtrar por rol">
            <option value="">Todos los roles</option>
            {(Object.keys(roleLabels) as Role[]).map((r) => <option key={r} value={r}>{roleLabels[r]}</option>)}
          </Select>
        </div>
      </div>

      <DataTable
        columns={columns}
        rows={list.data?.items}
        rowKey={(u) => u.id}
        loading={list.loading}
        error={list.error}
        onRetry={list.reload}
        empty={<EmptyState icon={<Users />} title="Sin resultados" description="Ningún usuario coincide con los filtros." />}
        footer={list.data && list.data.totalCount > 0 && <Pagination page={list.data.page} pageSize={PAGE_SIZE} totalPages={list.data.totalPages} totalCount={list.data.totalCount} onPageChange={setPage} />}
      />

      {creating && <CreateUserDrawer onClose={() => setCreating(false)} onSaved={() => { setCreating(false); list.reload() }} />}
      {editing && <EditRoleDialog user={editing} onClose={() => setEditing(null)} onSaved={() => { setEditing(null); list.reload() }} />}
      {resetting && <ResetPasswordDialog user={resetting} onClose={() => setResetting(null)} />}

      <ConfirmDialog
        open={toggling !== null}
        onClose={() => setToggling(null)}
        onConfirm={handleToggle}
        tone={toggling?.isActive ? 'danger' : 'primary'}
        title={toggling?.isActive ? 'Desactivar usuario' : 'Reactivar usuario'}
        confirmLabel={toggling?.isActive ? 'Desactivar' : 'Reactivar'}
        description={toggling?.isActive
          ? <><strong className="text-fg">{toggling?.email}</strong> no podrá iniciar sesión. Sus ventas y movimientos se conservan.</>
          : <><strong className="text-fg">{toggling?.email}</strong> podrá volver a iniciar sesión.</>}
      />
    </>
  )
}

/** Contraseña aleatoria que cumple la política (mayúscula, minúscula, número y símbolo). */
function generatePassword() {
  const pick = (chars: string) => chars[crypto.getRandomValues(new Uint32Array(1))[0] % chars.length]
  const upper = 'ABCDEFGHJKLMNPQRSTUVWXYZ', lower = 'abcdefghijkmnpqrstuvwxyz', digits = '23456789', symbols = '!@#$%*?'
  const all = upper + lower + digits + symbols
  const chars = [pick(upper), pick(lower), pick(digits), pick(symbols), ...Array.from({ length: 8 }, () => pick(all))]
  return chars.sort(() => crypto.getRandomValues(new Uint32Array(1))[0] / 2 ** 32 - 0.5).join('')
}

function RolePicker({ value, onChange }: { value: Role; onChange: (role: Role) => void }) {
  return (
    <div role="radiogroup" aria-label="Rol" className="grid gap-2">
      {(Object.keys(roleMeta) as Role[]).map((role) => (
        <button
          key={role}
          type="button"
          role="radio"
          aria-checked={value === role}
          onClick={() => onChange(role)}
          className={cn(
            'flex items-start gap-3 rounded-xl border p-3.5 text-left transition-all',
            value === role ? 'border-primary bg-primary-soft/60 ring-3 ring-ring' : 'border-border hover:border-border-strong hover:bg-surface-hover',
          )}
        >
          <span className={cn('rounded-lg p-2 [&_svg]:size-4', value === role ? 'bg-primary text-white' : 'bg-surface-muted text-fg-muted')}>{roleMeta[role].icon}</span>
          <span>
            <span className="block text-sm font-medium">{roleLabels[role]}</span>
            <span className="block text-xs text-fg-muted">{roleMeta[role].description}</span>
          </span>
        </button>
      ))}
    </div>
  )
}

function PasswordInput({ id, value, onChange, error }: { id: string; value: string; onChange: (v: string) => void; error?: string }) {
  return (
    <Field label="Contraseña" htmlFor={id} error={error} hint="Mínimo 8 caracteres con mayúscula, minúscula, número y símbolo." required>
      <Input
        id={id}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        invalid={!!error}
        className="font-mono"
        autoComplete="new-password"
        rightSlot={
          <button type="button" onClick={() => onChange(generatePassword())} className="flex items-center gap-1 rounded-md px-2 py-1 text-xs font-medium text-primary hover:bg-primary-soft">
            <Sparkles className="size-3.5" /> Generar
          </button>
        }
      />
    </Field>
  )
}

function CreateUserDrawer({ onClose, onSaved }: { onClose: () => void; onSaved: () => void }) {
  const [form, setForm] = useState<{ email: string; password: string; role: Role }>({ email: '', password: '', role: 'Cashier' })
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true); setError(null); setFieldErrors({})
    try {
      const user = await usersApi.create(form)
      toast.success(`Usuario ${user.email} creado`, { description: 'Comparte la contraseña de forma segura.' })
      onSaved()
    } catch (err) {
      const fields = getFieldErrors(err)
      setFieldErrors(fields)
      if (Object.keys(fields).length === 0) setError(getErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Drawer
      open
      onClose={onClose}
      title="Nuevo usuario"
      description="Crea la cuenta y asigna su rol."
      footer={<><Button variant="secondary" onClick={onClose}>Cancelar</Button><Button type="submit" form="user-form" loading={saving}>Crear usuario</Button></>}
    >
      <form id="user-form" onSubmit={handleSubmit} className="space-y-5" noValidate>
        {error && <Alert>{error}</Alert>}
        <Field label="Correo electrónico" htmlFor="user-email" error={fieldErrors.email} required>
          <Input id="user-email" type="email" autoFocus value={form.email} onChange={(e) => { setForm({ ...form, email: e.target.value }); setFieldErrors((errors) => omit(errors, 'email')) }} invalid={!!fieldErrors.email} placeholder="nombre@retailstock.com" autoComplete="off" />
        </Field>
        <PasswordInput id="user-password" value={form.password} onChange={(password) => { setForm({ ...form, password }); setFieldErrors((errors) => omit(errors, 'password')) }} error={fieldErrors.password} />
        <div>
          <p className="mb-2 text-[13px] font-medium">Rol</p>
          <RolePicker value={form.role} onChange={(role) => setForm({ ...form, role })} />
        </div>
      </form>
    </Drawer>
  )
}

function EditRoleDialog({ user, onClose, onSaved }: { user: User; onClose: () => void; onSaved: () => void }) {
  const [role, setRole] = useState<Role>(user.role)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function save() {
    setSaving(true); setError(null)
    try {
      await usersApi.update(user.id, { role, isActive: user.isActive })
      toast.success(`Rol de ${user.email} actualizado a ${roleLabels[role]}`)
      onSaved()
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Dialog
      open
      onClose={onClose}
      title="Editar rol"
      description={user.email}
      footer={<><Button variant="secondary" onClick={onClose}>Cancelar</Button><Button onClick={save} loading={saving} disabled={role === user.role}>Guardar</Button></>}
    >
      <div className="space-y-4">
        {error && <Alert>{error}</Alert>}
        <RolePicker value={role} onChange={setRole} />
      </div>
    </Dialog>
  )
}

function ResetPasswordDialog({ user, onClose }: { user: User; onClose: () => void }) {
  const [password, setPassword] = useState(generatePassword)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string>()

  async function save(e: FormEvent) {
    e.preventDefault()
    setSaving(true); setError(undefined)
    try {
      await usersApi.resetPassword(user.id, password)
      toast.success('Contraseña restablecida', { description: `Nueva contraseña de ${user.email}: ${password}` })
      onClose()
    } catch (err) {
      setError(getFieldErrors(err).newPassword ?? getErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Dialog
      open
      onClose={onClose}
      title="Restablecer contraseña"
      description={<>Asigna una contraseña nueva a <strong className="text-fg">{user.email}</strong>.</>}
      footer={<><Button variant="secondary" onClick={onClose}>Cancelar</Button><Button type="submit" form="reset-form" loading={saving}>Restablecer</Button></>}
    >
      <form id="reset-form" onSubmit={save} noValidate>
        <PasswordInput id="new-password" value={password} onChange={setPassword} error={error} />
      </form>
    </Dialog>
  )
}
