using UnityEngine;
using System.Collections;
using System;

namespace GcTypes {

    public static class UplinkFlags
    {
        public const int UPLINK_FLAG_REVERSE = 0x01;
    }

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
        CMD_EXT_SET_BOUNDING_BOX         = 0xAF,

        CMD_EXT_SET_ACTIVE               = 0xB0,
        CMD_EXT_RESET_TRACKING_TO_FRAME  = 0xB1,
        CMD_EXT_VIDEO                    = 0xB2,
        CMD_EXT_TEXT                     = 0xB3,

        CMD_RESET_TRACKING_TO_SETUP      = 0xB4,
        CMD_RESET_HOME_TO_SETUP          = 0xB5,
        CMD_EXT_SET_HOME_POSITION_COORDINATE = 0xB6,
        CMD_EXT_VEHICLECMD               = 0xB7,
		CMD_EXT_VIDEO_CMD				 = 0xB8,

        EXT_PARAM_1                      = 0xC0,
        EXT_PARAM_2                      = 0xC1,
        EXT_PARAM_ACK                    = 0xC2,

        CMD_EXT_TIMECODE                 = 0xD0,
        CMD_EXT_TIMECODE_ACTIVE          = 0xD1,

        CMD_EXT_WAYPOINT_FOLLOW_COLOR    = 0xD2,
    }

    public enum DroneState : int
    {
        // ATTENTION: NEVER CHANGE THIS ENUM!
        // ATTENTION: ^^^^^^^^^^^^^^^^^^^^^^^
        // ATTENTION: If you have to, also update 'QuickStateUi::ItemModel' and the
        // ATTENTION: state description in the call to 'StateTracker::Module::registerState()'
        // ATTENTION: call for State::DRONE_LOCAL_STATE, and be aware of the fact that you
        // ATTENTION: are probably invalidating all state recordings.
        NONE = 0,
        PUPPET,
        RESET_GEOFENCE,
        SET_GEOFENCE,
        GUESS_STATE,
        ON_GROUND,
        TAKEOFF_WAIT,
        TAKEOFF_MOTORS_ON,
        TAKEOFF_LOW_LEVEL,
        TAKEOFF,
        FOLLOW_TARGET,
        LANDING_INITIATE,
        LANDING_WAIT,
        LANDING_PREPARE,
        LANDING_FAST,
        LANDING_SLOW,
        LANDING_MOTORS_OFF,
        // overall item count
        COUNT
    };


    public enum ObjectInfoFlags
    {
        FLAG_IDLE =     0x01,
        FLAG_ACTIVE =     0x02
    }
    

    public struct ObjectInfo
    {
        public UInt16 id;
        public byte type;
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
        // orientation as quaternion
        public float trackingOrientationX;
        public float trackingOrientationY;
        public float trackingOrientationZ;
        public float trackingOrientationW;

        public UInt32 frameCount;
        public DroneState currentState;

        public UInt32 flags;
        public float relativeRotation;
    }

    public struct VideoInfo
    {
        public byte type;
        public UInt16 droneId;
        public UInt16 videoId;
        public byte side;
        public UInt16 startVideoFrame;
        public UInt16 startAnimFrame;
        public UInt16 stopAnimFrame;
        public byte videoEffect;
        public UInt16 brightness;
        public byte videoProjectionMode;

        public float x0;
        public float y0;
        public float x1;
        public float y1;
    }

	public enum VideoCmdType
	{
		STOP = 0,
		PAUSE,
		PLAY,
		PLAY_ONCE,
        START_LOOP,
        START_ONCE,
		COUNT
	};

    public enum DroneType
    {
        UAV = 0,
        UGV,
        UNKNOWN,
        COUNT
    };

    public class Drone
    {
        int id;
        bool idle;

        public Drone()
        {
            id = -1;
            idle = true;
        }

        public Drone(int _id)
        {
            id = _id;
            idle = true;
        }
    }
            
}
