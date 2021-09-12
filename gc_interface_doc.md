# Ground Control | External Interface - Guide for Application Developers

GroundControl provides a set of interfaces for applications to receive updates and send commands via LAN. The purpose is to allow GroundControl to be regarded as a "black box": Developing new swarm control and interaction scenarios need not be integrated into its core codebase; this makes the system more secure and more manageable. Furthermore, such scenarios can be developed in whichever language and framework is most appropriate for the project.

A Unity project using the interfaces is provided as an example, and as a foundation to fast-track the development of new scenarios.

## Drone Pose Broadcast via UDP

Drone pose data is sent to the local subnet broadcast address (e.g. `192.168.0.255`). The full array of drone pose data is sent out in a single UDP package every frame (currently 72 bytes per drone). The structs (see below) are tightly packed. The drone ID can be deduced from the struct's position in the array.

**Developer Note**:
To enable this feature in GroundControl, add `SimpleStateServer` to Build Options

### Protocol (Unity/C# code)

```csharp
public struct ObjectInfo
{
	public UInt16 id;

	/// currently: 0 for UAV, 1 for UGV, 2 for unknown
	public byte type;
	
	/// 0: undecided, 1: live, 2: simulated
	public byte liveness;
	
	public float trackingX, trackingY, trackingZ;
	// CCW tracking yaw angle [deg]
	public float trackingOrientation;
	// waypoint position relative to site position [m]
	public float targetX, targetY, targetZ;
	// CCW waypoint yaw angle [deg]
	public float targetOrientation;
	// LED color [0-1]
	public float colorR, colorG, colorB;
	// data timestamp [s]
	public float timestamp;
	public float trackingOrientationX;
	public float trackingOrientationY;
	public float trackingOrientationZ;
	public float trackingOrientationW;

	public UInt32 frameCount;
	
	// Ground Control drone state
	public DroneState currentState;

	// a bitmask. idle (= ready for commands): 0x01; active (= enabled in Ground Control): 0x02
	public UInt32 flags;
}
```

## State Server

The state server can establish a two-way communication with an arbitrary number of clients via TCP. Each client is either *privileged* or not (configurable through a client IP list on GroundControl startup); privileged clients can execute all commands provided via the State Server command protocol, non-privileged clients can execute a subset.

The protocol is detailed below, along with the full list of commands. The commands beginning with `CMD_EXT` are non-privileged, the rest are privileged.

**Developer Note**:
To enable this feature in GroundControl, check *Enable Networking* on startup.

### Protocol (Unity/C# code)

```csharp
class NetCmdRecord 
{
    public Int32 type; // NetCmdType, see below
    public Int32 context;
    public Int32 intParam1;
    public Int32 intParam2;
}

// the following subclasses emulate a C union in C#

class NetCmdRecordFloat : NetCmdRecord
{
    public float[] floatVecParam;

    public NetCmdRecordFloat()
    {
        floatVecParam = new float[4];
    }
}

class NetCmdRecordDouble : NetCmdRecord
{
    public double[] doubleVecParam;

    public NetCmdRecordDouble()
    {
        doubleVecParam = new double[2];
    }
}

class NetCmdRecordLong : NetCmdRecord
{
    public long[] longVecParam;

    public NetCmdRecordLong()
    {
        longVecParam = new long[2];
    }
}
```

### Commands (Unity/C# code)

```csharp
public enum NetCmdType
	{
		PING_REQUEST                     = 0x00,
		PING_RESPONSE                    = 0x01,
		DATA_BLOB                        = 0x02,
		SERVER_INFO                      = 0x03,
		CLIENT_INFO                      = 0x04,
        ACK_CMD_TIMEOUT                  = 0x05,
        ACK_CMD_SUCCESS                  = 0x06,
        // -----
        CMD_CTRL_MODE                    = 0x10,
		CMD_COLOR_MODE                   = 0x11,
		CMD_MANUAL_COLOR_MODE            = 0x12,
		// -----
		CMD_RESET_HOME_POSITION          = 0x20, // => CMD_MOVE_HOME_POSITION_TO_DRONE
		CMD_RESET_HOME_POSITION_TO_ANIM  = 0x21,
		// -----
		CMD_RESET_ANIM_POSITION          = 0x30,
		CMD_RESET_ANIM_POSITION_TO_DRONE = 0x31, // => CMD_MOVE_ANIM_POSITION_TO_DRONE
		// -----
		CMD_EMERGENCY_MODE               = 0x40,
		CMD_ENABLE_SAFETY_PILOT          = 0x41,
		CMD_WAKE_UP                      = 0x42,
		CMD_MOTORS_CMD                   = 0x43,
		// -----
		CMD_SET_ACTIVE                   = 0x50,
		CMD_SET_MANUAL_TARGET            = 0x51,
		CMD_SET_GROUP                    = 0x52,
		// -----
		CMD_SET_ANIM_PLAYBACK_TIME       = 0x60,
		CMD_SET_ANIM_PLAYBACK_SPEED      = 0x61,
		CMD_SET_ANIM_COLOR_INTENSITY     = 0x62,
		// -----
		CMD_SET_CALIBRATION_OFFSET       = 0x70,
		// -----
		CMD_SET_GEOFENCE                 = 0x80,
		CMD_SET_GEOFENCE_POSITION        = 0x81,
		CMD_RECONFIGURE_GEOFENCE         = 0x82,
		// -----
		CMD_SET_CLOCK_TIMEOUT            = 0x90,
		// -----
		CMD_EXT_GO_EXTERNAL              = 0xA0,
		CMD_EXT_GO_HOME                  = 0xA1,
		CMD_EXT_GO_HOME_AT_POSITION      = 0xA2,
		CMD_EXT_PAUSE                    = 0xA3,
		CMD_EXT_COLOR                    = 0xA4,
		CMD_EXT_COLOR_GRADIENT           = 0xA5,
		CMD_EXT_WAYPOINT                 = 0xA6,
		CMD_EXT_WAYPOINT_FOLLOW          = 0xA7,
		CMD_EXT_PLAYBACK_ANIM            = 0xA8,
		CMD_EXT_ANIM_CANCEL_LOOP         = 0xA9,
		CMD_EXT_ANIM_ADJ_PLAYBACK_SPEED  = 0xAA,
		CMD_EXT_HALT                     = 0xAB,
		CMD_EXT_CHECK_ALL_IDLE           = 0xAC,
		CMD_EXT_HALT_AND_SAVE_CUR_CMD    = 0xAD,
		CMD_EXT_PUSH_SAVED_CMD           = 0xAE,
		CMD_EXT_SET_BOUNDING_BOX		 = 0xAF,

		CMD_EXT_SET_ACTIVE			     = 0xB0,
		CMD_EXT_RESET_TRACKING_TO_FRAME  = 0xB1,
		CMD_EXT_VIDEO					 = 0xB2,
        CMD_EXT_TEXT                     = 0xB3,

        CMD_RESET_TRACKING_TO_SETUP      = 0xB4,
        CMD_RESET_HOME_TO_SETUP          = 0xB5,
        CMD_EXT_SET_HOME_POSITION_COORDINATE = 0xB6,
        CMD_EXT_VEHICLECMD               = 0xB7,

        EXT_PARAM_1                      = 0xC0,
		EXT_PARAM_2                      = 0xC1,
        EXT_PARAM_ACK                    = 0xC2

	}
```

### Important Commands

To understand how these commands work, it is necessary to understand the GroundControl command queue. For each drone ID, there is a current command and a queue. Generally, new commands are pushed to the queue, and active commands remove themselves when "finishing" conditions are met for them. Note that some commands **must** be cancelled with one of the `HALT` commands because they do not have a finishing condition (e.g. a looped animation).

#### `CMD_EXT_GO_EXTERNAL`

Causes the given drones to take off.

#### `CMD_EXT_GO_HOME` and `CMD_EXT_GO_HOME_AT_POSITION`

Causes the given drones to land at their home position, or, in the second version, at their current position.

#### `CMD_EXT_PLAYBACK_ANIM`

Given drones play given animation frames. The animation loaded into GroundControl is used. *This is incompatible with the manual waypoint and colour commands.*

#### `CMD_EXT_WAYPOINT` and `CMD_EXT_WAYPOINT_FOLLOW`

Causes the given drones to head to the given waypoint. The `CMD_EXT_WAYPOINT_FOLLOW` version *replaces* another `CMD_EXT_WAYPOINT_FOLLOW` command if there is one, and it does not remove itself from the command queue when the target is reached. It should be used when a drone needs to "follow" a moving target, such as a tracked person or an object that is, e.g., animated in Unity. This means that it *must* be cancelled with `CMD_EXT_HALT`.

#### `CMD_EXT_HALT`

Clears current command and command queue.

#### `CMD_EXT_SET_ACTIVE`

Corresponds to clicking a drone's ID in `QuickStateUi`. Value: 0 or 1.

#### `CMD_EXT_RESET_TRACKING_TO_FRAME`

Re-initializes the tracking system for the given drone. The system looks for matching clusters in a 0.5m radius around the drone's position at the given frame. Values: drone ID, frame number.

#### `CMD_EXT_VIDEO`

Starts video playback. The start-video command is sent only once, but saved in GC. When a `CMD_EXT_PLAYBACK_ANIM` is activated and `useColor` is set to true for it, the current video playback frame is continuously matched with the animation playback time (assuming that the first animation frame and the first video frame should be matched) so as to keep them in sync.

### `CMD_EXT_TEXT`

Sends a text message to be displayed on a drone-mounted screen.

### `CMD_EXT_SET_HOME_POSITION_COORDINATE`

Sets the home position of a vehicle to a given coordinate. This is important for the new Free Mode where animations are not stored in Ground Control anymore, but starting positions may have to be matched to an animation loaded into UnityCtrl.

### `CMD_EXT_VEHICLECMD`

Vehicle-specific commands such as *Rotate Base*.

#### `PING_REQUEST` and `PING_RESPONSE`

Used to keep track of the connection status. See Unity example project for recommended use.