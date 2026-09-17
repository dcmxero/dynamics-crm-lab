import * as React from "react";
import { Stage, StageState } from "./workOrderStatus";

export interface StatusTrackProps {
    readonly stages: readonly Stage[];
    readonly label: string;
}

const DOT_COLORS: Record<StageState, string> = {
    done: "#107c10",
    current: "#0f6cbd",
    upcoming: "#c8c6c4"
};

/**
 * Draws the lifecycle as a row of stages with the current one picked out.
 *
 * Presentation only: everything arrives through props, so there is no state to
 * get out of step with the record.
 */
export const StatusTrack: React.FC<StatusTrackProps> = ({ stages, label }) => {
    const currentIndex = stages.findIndex(stage => stage.state === "current");

    return (
        <div
            role="group"
            aria-label={label}
            style={{
                display: "flex",
                alignItems: "flex-start",
                fontFamily: "Segoe UI, sans-serif",
                fontSize: 12
            }}
        >
            {stages.map((stage, index) => (
                <div key={stage.value} style={{ flex: 1, minWidth: 0 }}>
                    <div style={{ display: "flex", alignItems: "center" }}>
                        <span
                            aria-hidden="true"
                            style={{
                                width: 10,
                                height: 10,
                                borderRadius: "50%",
                                background: DOT_COLORS[stage.state],
                                flex: "0 0 auto"
                            }}
                        />
                        {index < stages.length - 1 && (
                            <span
                                aria-hidden="true"
                                style={{
                                    height: 2,
                                    flex: 1,
                                    background: index < currentIndex ? DOT_COLORS.done : DOT_COLORS.upcoming
                                }}
                            />
                        )}
                    </div>
                    <div
                        style={{
                            marginTop: 4,
                            paddingRight: 8,
                            color: stage.state === "upcoming" ? "#605e5c" : "#201f1e",
                            fontWeight: stage.state === "current" ? 600 : 400,
                            overflow: "hidden",
                            textOverflow: "ellipsis",
                            whiteSpace: "nowrap"
                        }}
                        aria-current={stage.state === "current" ? "step" : undefined}
                    >
                        {stage.label}
                    </div>
                </div>
            ))}
        </div>
    );
};
