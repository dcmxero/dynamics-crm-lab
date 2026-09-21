/**
 * The shapes the API hands out.
 *
 * Statuses arrive as names rather than numbers, so a renumbered choice column
 * in Dataverse cannot silently change what the client understands.
 */

export const WORK_ORDER_STATUSES = ['New', 'Assigned', 'InProgress', 'Closed'] as const;

export type WorkOrderStatus = (typeof WORK_ORDER_STATUSES)[number];

export const WORK_ORDER_STATUS_LABELS: Record<WorkOrderStatus, string> = {
  New: 'New',
  Assigned: 'Assigned',
  InProgress: 'In progress',
  Closed: 'Closed',
};

export interface WorkOrderSummary {
  readonly id: string;
  readonly number: string;
  readonly status: WorkOrderStatus;
  readonly technicianId: string | null;
  readonly totalPrice: number;
  readonly currency: string;
  readonly lineCount: number;
}

export interface WorkOrderLine {
  readonly id: string;
  readonly description: string;
  readonly quantity: number;
  readonly unitPrice: number;
  readonly lineTotal: number;
}

export interface WorkOrder {
  readonly id: string;
  readonly number: string;
  readonly customerId: string;
  readonly equipmentId: string;
  readonly technicianId: string | null;
  readonly status: WorkOrderStatus;
  readonly resolution: string | null;
  readonly totalPrice: number;
  readonly currency: string;
  readonly lines: readonly WorkOrderLine[];
}

export interface RaiseWorkOrderRequest {
  readonly customerId: string;
  readonly equipmentId: string;
  readonly lines: readonly {
    readonly description: string;
    readonly quantity: number;
    readonly unitPrice: number;
  }[];
}

export interface WorkOrderCreated {
  readonly id: string;
  readonly number: string;
  readonly status: WorkOrderStatus;
  readonly totalPrice: number;
  readonly currency: string;
}

export interface WorkOrderAssigned {
  readonly id: string;
  readonly technicianId: string;
  readonly technicianName: string;
}

export interface WorkOrderStarted {
  readonly id: string;
  readonly number: string;
  readonly status: WorkOrderStatus;
}

export interface WorkOrderClosed {
  readonly id: string;
  readonly number: string;
  readonly totalPrice: number;
  readonly currency: string;
}

/**
 * A failure the API reported, already reduced to something worth showing.
 *
 * `rule` marks the 422 case: the request was fine, the job simply does not
 * allow it. That reads differently to the user than a network problem, so the
 * two are not collapsed into one message.
 */
export interface ApiFailure {
  readonly message: string;
  readonly rule: boolean;
  readonly status: number;
}
