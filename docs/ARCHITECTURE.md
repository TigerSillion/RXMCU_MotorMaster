# RXMCU_MotorMaster Architecture

## 1. Layers
- UI: `MainWindow.xaml` + ScottPlot
- ViewModel: `ViewModels/*`
- Event hub: `Messaging/AppEventBus.cs` (UI-thread marshaling)
- Transport: `Services/ITransportService`, `Services/UartTransport.cs`, `Services/TransportSimulator.cs`
- Protocol codec: `Services/UartFrameCodec.cs`
- Persistence: `Services/SessionService.cs`
- Models: `Models/*`

## 2. Runtime Flow
1. `ConnectionViewModel` opens UART and executes `HELLO`.
2. `ConnectionViewModel` starts heartbeat polling (`~800ms`).
3. `ScopeViewModel` requests layout and controls stream start/stop.
4. `UartTransport` decodes frame stream and dispatches:
   - response by `seq` waiter
   - event log to bus log
   - event scope to `SampleBatch`
5. `ParamViewModel` uses typed read/write with readback verify.
6. `MainWindow` renders waveform from `ScopeViewModel` ring buffers.

## 3. Protocol Boundary
- `UartFrameCodec` contains frame encode/decode and payload parser helpers.
- `UartTransport` owns serial I/O, waiter dictionary, and event routing.
- ViewModels do not parse wire bytes directly.

## 4. Safety and Robustness
- CRC checked before frame accept.
- Unknown/invalid frames are ignored and logged.
- Parameter write keeps range check + optional second confirmation.
- ObservableCollection updates are always on UI thread.

## 5. Current Extensibility Points
- Add map/elf parser service to auto-populate parameter table.
- Add block read/write UI panel.
- Add command macro sequences for startup/stop scripts.
