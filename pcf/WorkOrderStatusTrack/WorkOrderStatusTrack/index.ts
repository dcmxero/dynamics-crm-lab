import * as React from "react";
import { IInputs, IOutputs } from "./generated/ManifestTypes";
import { StatusTrack } from "./StatusTrack";
import { describeStages } from "./workOrderStatus";

/**
 * Binds the control to the Power Apps component framework.
 *
 * The class does nothing but talk to the host: read the bound value, hand back
 * the outputs. Working out the stages lives in workOrderStatus.ts and drawing
 * them in StatusTrack.tsx.
 */
export class WorkOrderStatusTrack implements ComponentFramework.ReactControl<IInputs, IOutputs> {
    private status: number | null = null;

    public init(
        context: ComponentFramework.Context<IInputs>,
        notifyOutputChanged: () => void,
        state: ComponentFramework.Dictionary
    ): void {
        // A virtual control owns no DOM; the host renders whatever updateView returns.
    }

    public updateView(context: ComponentFramework.Context<IInputs>): React.ReactElement {
        this.status = context.parameters.status.raw ?? null;

        return React.createElement(StatusTrack, {
            stages: describeStages(this.status),
            label: context.parameters.status.attributes?.DisplayName ?? "Work order status"
        });
    }

    public getOutputs(): IOutputs {
        // The control only shows the value, so it hands it back unchanged.
        return { status: this.status ?? undefined };
    }

    public destroy(): void {
        // No listeners or timers were attached, so there is nothing to tidy up.
    }
}
