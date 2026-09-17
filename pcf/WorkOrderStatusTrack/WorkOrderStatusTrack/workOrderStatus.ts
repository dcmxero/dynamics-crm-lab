/**
 * The lifecycle of a work order, mirrored from the domain enum so the control
 * shows the same stages the server enforces.
 *
 * Deliberately free of React and of the Power Apps framework: it is plain data
 * and plain functions, which makes it the part that can be reasoned about and
 * tested on its own.
 */

export const WORK_ORDER_STAGES = [
    { value: 1, label: "New" },
    { value: 2, label: "Assigned" },
    { value: 3, label: "In progress" },
    { value: 4, label: "Closed" }
] as const;

export type StageState = "done" | "current" | "upcoming";

export interface Stage {
    readonly value: number;
    readonly label: string;
    readonly state: StageState;
}

/**
 * Describes each stage relative to the one the job has reached.
 *
 * An unknown or missing value leaves every stage upcoming rather than guessing,
 * so a choice column that gained a new option does not silently mislead.
 */
export function describeStages(current: number | null | undefined): Stage[] {
    const currentIndex = WORK_ORDER_STAGES.findIndex(stage => stage.value === current);

    return WORK_ORDER_STAGES.map((stage, index) => ({
        value: stage.value,
        label: stage.label,
        state: stateOf(index, currentIndex)
    }));
}

/**
 * Reports how far along the lifecycle the job is, as a fraction between 0 and 1.
 */
export function progressOf(current: number | null | undefined): number {
    const currentIndex = WORK_ORDER_STAGES.findIndex(stage => stage.value === current);

    if (currentIndex < 0) {
        return 0;
    }

    return currentIndex / (WORK_ORDER_STAGES.length - 1);
}

function stateOf(index: number, currentIndex: number): StageState {
    if (currentIndex < 0 || index > currentIndex) {
        return "upcoming";
    }

    return index === currentIndex ? "current" : "done";
}
