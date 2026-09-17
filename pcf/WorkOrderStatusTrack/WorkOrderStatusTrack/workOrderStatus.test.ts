import { describe, expect, it } from "vitest";
import { describeStages, progressOf, WORK_ORDER_STAGES } from "./workOrderStatus";

describe("describeStages", () => {
    it("marks earlier stages done and the reached one current", () => {
        const stages = describeStages(3);

        expect(stages.map(stage => stage.state)).toEqual(["done", "done", "current", "upcoming"]);
    });

    it("marks only the first stage current for a new job", () => {
        expect(describeStages(1)[0].state).toBe("current");
    });

    it("marks every stage done or current once the job is closed", () => {
        const stages = describeStages(4);

        expect(stages.some(stage => stage.state === "upcoming")).toBe(false);
    });

    it("leaves every stage upcoming for a value it does not know", () => {
        for (const value of [null, undefined, 0, 99]) {
            const stages = describeStages(value);

            expect(stages.every(stage => stage.state === "upcoming")).toBe(true);
        }
    });

    it("returns one entry per stage", () => {
        expect(describeStages(2)).toHaveLength(WORK_ORDER_STAGES.length);
    });
});

describe("progressOf", () => {
    it("runs from nothing to complete across the lifecycle", () => {
        expect(progressOf(1)).toBe(0);
        expect(progressOf(4)).toBe(1);
    });

    it("reports no progress for a value it does not know", () => {
        expect(progressOf(99)).toBe(0);
    });
});
