# RXMCU_MotorMaster Product Requirements

## 1. Purpose
Provide a production-usable UART GUI for RX26T motor control debugging and tuning, aligned with MCU UART5 protocol v1.0.

## 2. Functional Scope (Current)
- FR-01: COM/baud connect-disconnect.
- FR-02: HELLO capability negotiation and display.
- FR-03: Periodic HEARTBEAT monitor (tick/mode/rxDrop/txDrop).
- FR-04: Scope dynamic layout fetch and real-time waveform display.
- FR-05: Scope start/stop command with event-stream data plotting.
- FR-06: Address+type parameter read and write.
- FR-07: Write readback verification and operation log.
- FR-08: Motor command pad (`RUN/STOP/RESET` via `MOTOR_CTRL`).
- FR-09: Event log pane (`EVT_LOG_TEXT`).
- FR-10: Session save/load and CSV export.

## 3. Safety Requirements
- SR-01: Write range validation in UI (`min/max`).
- SR-02: SafetyLevel >=2 requires second confirmation.
- SR-03: Failed protocol response must not update parameter as success.

## 4. Non-Functional
- NFR-01: UI remains responsive during scope events.
- NFR-02: No crash on malformed CRC/length frame.
- NFR-03: Cross-thread UI update safety guaranteed.

## 5. Next Iteration
- NR-01: MAP/ELF parser for automatic symbol/type import.
- NR-02: Block read/write tool panel and hex inspector.
- NR-03: Auto test script panel (startup sequence + assertions).
