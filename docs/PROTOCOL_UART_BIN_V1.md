# UART5 Protocol Integration (Implemented)

## 1. Frame Layout
- SOF: `0x55 0xAA`
- Header: `VER(u8) TYPE(u8) SEQ(u16) CMD(u16) LEN(u16)`
- Payload: `LEN` bytes
- CRC16: CCITT (`poly=0x1021`, `init=0xFFFF`) over `[VER..PAYLOAD]`

## 2. Types
- `TYPE=0x01` request
- `TYPE=0x02` response
- `TYPE=0x03` event (async)

## 3. Commands in GUI
- `0x0001 HELLO`
- `0x0002 HEARTBEAT`
- `0x0100 MOTOR_CTRL`
- `0x0300 READ_TYPED`
- `0x0301 WRITE_TYPED`
- `0x0400 SCOPE_LAYOUT`
- `0x0401 SCOPE_CTRL`
- `0x8001 EVT_LOG_TEXT`
- `0x8400 EVT_SCOPE_DATA`

## 4. Response
- `payload = status:u8 | err:u8 | detail:u16 | data...`
- GUI behavior:
  - `status==0`: success
  - `status!=0`: fail and log error

## 5. Implemented Parsing
- HELLO data: `proto_ver:u16 + caps:u32`
- HEARTBEAT data: `loop_tick:u32 + system_mode:u8 + rx_drop:u16 + tx_drop:u16`
- Scope layout: dynamic channel metadata
- Scope event: `seq:u32 + ch_count:u8 + values:f32[ch_count]`

## 6. Typed Access
- `READ_TYPED`: `addr:u32 + type:u8 + count:u16`
- `WRITE_TYPED`: `addr:u32 + type:u8 + count:u16 + raw`
- GUI currently supports single-value read/write and readback verify.

## 7. Notes
- Event log is fully protocolized; no dependency on raw text terminal parsing.
- `SCOPE_START/STOP` uses request/response path, stream data uses event path.
- UI event updates are marshaled onto WPF UI thread in `AppEventBus`.
