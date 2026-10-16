import { CircleCheck, Printer, ShoppingCart } from 'lucide-react'
import { Button } from '../../components/ui/Button'
import { Dialog } from '../../components/ui/Dialog'
import { Logo } from '../../components/ui/Logo'
import { formatDateTime, formatMoney } from '../../lib/format'
import { TAX_RATE_PERCENT } from './cart'
import type { Sale } from './salesApi'

/** Comprobante de la venta con los montos OFICIALES de la API. "Imprimir" usa los estilos @media print. */
export function ReceiptDialog({ sale, onClose, onNewSale }: { sale: Sale; onClose: () => void; onNewSale?: () => void }) {
  return (
    <Dialog
      open
      onClose={onClose}
      title={<span className="flex items-center gap-2"><CircleCheck className="size-5 text-emerald-500" /> Venta {sale.number}</span>}
      description={`${formatDateTime(sale.date)} · ${sale.warehouseName}`}
      footer={
        <>
          <Button variant="secondary" icon={<Printer className="size-4" />} onClick={() => window.print()}>Imprimir</Button>
          {onNewSale
            ? <Button icon={<ShoppingCart className="size-4" />} onClick={onNewSale}>Nueva venta</Button>
            : <Button onClick={onClose}>Cerrar</Button>}
        </>
      }
    >
      <SaleReceipt sale={sale} />
    </Dialog>
  )
}

export function SaleReceipt({ sale }: { sale: Sale }) {
  return (
    <div className="print-area text-sm">
      <div className="mb-4 hidden print:block"><Logo /></div>
      <div className="grid grid-cols-2 gap-3 rounded-xl border border-border bg-surface-muted/50 p-3.5 text-[13px]">
        <div><p className="text-xs text-fg-muted">Cliente</p><p className="font-medium">{sale.customerName}</p><p className="text-xs text-fg-muted">{sale.customerId}</p></div>
        <div><p className="text-xs text-fg-muted">Cajero</p><p className="truncate font-medium">{sale.userEmail}</p><p className="text-xs text-fg-muted">{sale.warehouseName}</p></div>
      </div>
      <table className="mt-4 w-full">
        <thead className="text-left text-xs text-fg-muted">
          <tr><th className="pb-2 font-medium">Producto</th><th className="pb-2 text-right font-medium">Cant.</th><th className="pb-2 text-right font-medium">Total</th></tr>
        </thead>
        <tbody className="divide-y divide-border border-y border-border">
          {sale.details.map((d) => (
            <tr key={d.productId}>
              <td className="py-2"><p className="font-medium">{d.productName}</p><p className="text-xs text-fg-muted">{formatMoney(d.unitPrice)} c/u</p></td>
              <td className="tabular py-2 text-right">{d.quantity}</td>
              <td className="tabular py-2 text-right font-medium">{formatMoney(d.lineTotal)}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <dl className="tabular mt-4 space-y-1.5">
        <div className="flex justify-between text-fg-muted"><dt>Subtotal</dt><dd>{formatMoney(sale.subtotal)}</dd></div>
        <div className="flex justify-between text-fg-muted"><dt>IVA {TAX_RATE_PERCENT}%</dt><dd>{formatMoney(sale.tax)}</dd></div>
        <div className="flex justify-between border-t border-border pt-2 text-lg font-semibold"><dt>Total</dt><dd>{formatMoney(sale.total)}</dd></div>
      </dl>
    </div>
  )
}
