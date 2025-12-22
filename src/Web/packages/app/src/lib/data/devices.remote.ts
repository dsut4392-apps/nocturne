import { getRequestEvent, query } from '$app/server';
import type { DeviceStatus } from '$lib/api/generated/nocturne-api-client';

/**
 * Interface for a processed device ready for display
 */
export interface DisplayDevice {
  id: string; /** The device status ID */
  name: string; /** Name of the device or uploader */
  type: "pump" | "cgm" | "uploader" | "loop" | "unknown";
  lastSeen: Date;
  status: "active" | "stale";
  batteryLevel?: number; /** 0-100 */
  details: {
    label: string;
    value: string | number;
    unit?: string;
  }[];
  raw: DeviceStatus;
}

/**
 * Fetches the most recent device statuses and aggregates them by device name/uploader.
 * Returns a list of unique devices with their most recent status.
 */
export const getRecentDeviceStatuses = query(async () => {
  const { locals } = getRequestEvent();
  const { apiClient } = locals;

  // Fetch the last 50 device statuses to get a good sampling of active devices
  try {
    const response = await apiClient.deviceStatus.getDeviceStatus(undefined);

    if (!response?.data) {
      return [];
    }

    const uniqueDevices = new Map<string, DisplayDevice>();

    // Sort data by created_at desc
    const sortedData = [...response.data].sort((a, b) => {
      const timeA = new Date(a.created_at || 0).getTime();
      const timeB = new Date(b.created_at || 0).getTime();
      return timeB - timeA;
    });

    for (const status of sortedData) {
      let deviceName = status.device;
      // Fallbacks for name
      if (!deviceName || deviceName === "openaps://AndroidAPS") {
          if (status.uploader?.name) deviceName = status.uploader.name;
          else if (status.loop?.name) deviceName = status.loop.name;
      }

      // Normalize key
      const key = deviceName || "Unknown Device";

      if (uniqueDevices.has(key)) continue;

      const displayDevice: DisplayDevice = mapStatusToDisplayDevice(status, key);
      uniqueDevices.set(key, displayDevice);
    }

    return Array.from(uniqueDevices.values());
  } catch (err) {
    console.error('Error fetching device statuses:', err);
    return [];
  }
});

function mapStatusToDisplayDevice(status: DeviceStatus, name: string): DisplayDevice {
  const lastSeen = new Date(status.created_at || Date.now());
  const isStale = (Date.now() - lastSeen.getTime()) > (1000 * 60 * 15); // 15 mins

  let type: DisplayDevice["type"] = "unknown";
  let batteryLevel: number | undefined;
  const details: DisplayDevice["details"] = [];

  // Detect type and extract details
  if (status.pump) {
    type = "pump";
    if (status.pump.battery?.percent !== undefined) {
      batteryLevel = status.pump.battery.percent;
    }

    if (status.pump.reservoir !== undefined) {
      details.push({ label: "Reservoir", value: status.pump.reservoir, unit: "U" });
    }

    if (status.pump.status?.status) {
      details.push({ label: "Status", value: status.pump.status.status });
    }

    if (status.pump.iob?.iob !== undefined) {
      details.push({ label: "IOB", value: status.pump.iob.iob.toFixed(2), unit: "U" });
    }
  } else if (status.cgm) {
    type = "cgm";
    if (status.cgm.transmitterBattery !== undefined) {
      batteryLevel = status.cgm.transmitterBattery;
    }

    if (status.cgm.sensorAge) {
      details.push({ label: "Sensor Age", value: status.cgm.sensorAge });
    }
    if (status.cgm.transmitterAge) {
      details.push({ label: "Transmitter Age", value: status.cgm.transmitterAge });
    }
    if (status.cgm.signalStrength !== undefined) {
      details.push({ label: "Signal", value: status.cgm.signalStrength, unit: "%" });
    }
  } else if (status.loop || status.openaps) {
    type = "loop";
    // Loop/OpenAPS often report uploader battery
    if (status.uploader?.battery !== undefined) {
      batteryLevel = status.uploader.battery;
    }

    const loop = status.loop || status.openaps;
    if (loop && 'iob' in loop && typeof loop.iob === 'object' && loop.iob && 'iob' in loop.iob) {
        // Safe cast or check properties, simplistic here
        // @ts-ignore - types are a bit complex with unions
        const iobVal = (loop.iob as any).iob;
        if (typeof iobVal === 'number') {
           details.push({ label: "IOB", value: iobVal.toFixed(2), unit: "U" });
        }
    }
    if (status.loop && status.loop.enacted) {
         if (status.loop.enacted.rate !== undefined) {
             details.push({ label: "Temp Basal", value: status.loop.enacted.rate, unit: "U/hr" });
         }
    }
  } else if (status.uploader) {
    type = "uploader";
    if (status.uploader.battery !== undefined) {
      batteryLevel = status.uploader.battery;
    }
  }

  return {
    id: status._id || Math.random().toString(),
    name,
    type,
    lastSeen,
    status: isStale ? "stale" : "active",
    batteryLevel,
    details,
    raw: status
  };
}
