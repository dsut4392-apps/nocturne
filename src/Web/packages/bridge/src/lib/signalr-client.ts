import {
  HubConnection,
  HubConnectionBuilder,
  LogLevel,
} from "@microsoft/signalr";
import { createHash } from "crypto";
import logger from "./logger.js";
import MessageTranslator from "./message-translator.js";

interface SignalRConfig {
  hubUrl: string;
  reconnectAttempts: number;
  reconnectDelay: number;
  maxReconnectDelay: number;
  apiSecret: string;
}

class SignalRClient {
  private messageHandler: MessageTranslator;
  private dataConnection: HubConnection | null = null;
  private alarmConnection: HubConnection | null = null;
  private reconnectAttempts: number = 0;
  private maxReconnectAttempts: number;
  private reconnectDelay: number;
  private maxReconnectDelay: number;
  private hubUrl: string;
  private apiSecret: string;
  private isConnecting: boolean = false;

  constructor(messageHandler: MessageTranslator, config: SignalRConfig) {
    this.messageHandler = messageHandler;
    this.hubUrl = config.hubUrl;
    this.maxReconnectAttempts = config.reconnectAttempts;
    this.reconnectDelay = config.reconnectDelay;
    this.maxReconnectDelay = config.maxReconnectDelay;
    this.apiSecret = config.apiSecret;
  }

  async connect(): Promise<void> {
    if (this.isConnecting) {
      logger.warn("SignalR connection attempt already in progress");
      return;
    }

    this.isConnecting = true;

    try {
      // Derive alarm hub URL from data hub URL
      // e.g., http://localhost:5000/hubs/data -> http://localhost:5000/hubs/alarms
      const alarmHubUrl = this.hubUrl.replace("/hubs/data", "/hubs/alarms");

      // Create Data Hub connection
      this.dataConnection = new HubConnectionBuilder()
        .withUrl(this.hubUrl)
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (retryContext) => {
            const delay = Math.min(
              this.reconnectDelay *
                Math.pow(2, retryContext.previousRetryCount),
              this.maxReconnectDelay,
            );
            logger.info(
              `SignalR DataHub reconnect attempt ${
                retryContext.previousRetryCount + 1
              } in ${delay}ms`,
            );
            return delay;
          },
        })
        .configureLogging(LogLevel.Information)
        .build();

      // Create Alarm Hub connection
      this.alarmConnection = new HubConnectionBuilder()
        .withUrl(alarmHubUrl)
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (retryContext) => {
            const delay = Math.min(
              this.reconnectDelay *
                Math.pow(2, retryContext.previousRetryCount),
              this.maxReconnectDelay,
            );
            logger.info(
              `SignalR AlarmHub reconnect attempt ${
                retryContext.previousRetryCount + 1
              } in ${delay}ms`,
            );
            return delay;
          },
        })
        .configureLogging(LogLevel.Information)
        .build();

      // Set up event handlers for both hubs
      this.setupDataHubEventHandlers();
      this.setupAlarmHubEventHandlers();

      // Start both connections
      await Promise.all([
        this.dataConnection.start(),
        this.alarmConnection.start(),
      ]);
      logger.info("SignalR connections established (DataHub + AlarmHub)");

      // Authenticate with the data hub to join the "authorized" group
      await this.authenticateWithDataHub();

      // Subscribe to storage collections to receive create/update/delete events
      await this.subscribeToStorageCollections();

      // Subscribe to alarm notifications
      await this.subscribeToAlarms();

      this.reconnectAttempts = 0;
      this.isConnecting = false;
    } catch (error) {
      logger.error("Failed to connect to SignalR hubs:", error);
      this.isConnecting = false;
      await this.handleReconnect();
    }
  }

  private setupDataHubEventHandlers(): void {
    if (!this.dataConnection) return;

    // Handle connection state changes
    this.dataConnection.onclose(() => {
      logger.warn("SignalR DataHub connection closed");
      this.handleReconnect();
    });

    this.dataConnection.onreconnecting(() => {
      logger.info("SignalR DataHub connection lost, attempting to reconnect...");
    });

    this.dataConnection.onreconnected(async () => {
      logger.info("SignalR DataHub connection reestablished");
      this.reconnectAttempts = 0;

      // Re-authenticate and re-subscribe after reconnection
      await this.authenticateWithDataHub();
      await this.subscribeToStorageCollections();
    });

    // Handle incoming messages from DataHub
    this.dataConnection.on("dataUpdate", (data: any) => {
      logger.debug("Received dataUpdate from SignalR:", data);
      this.messageHandler.handleDataUpdate(data);
    });

    this.dataConnection.on("statusUpdate", (status: any) => {
      logger.debug("Received statusUpdate from SignalR:", status);
      this.messageHandler.handleStatusUpdate(status);
    });

    // Handle storage events (create, update, delete)
    // These method names must match the WebSocketEvents enum in C#
    this.dataConnection.on("create", (data: any) => {
      logger.debug("Received create from SignalR:", data);
      this.messageHandler.handleStorageCreate(data);
    });

    this.dataConnection.on("update", (data: any) => {
      logger.debug("Received update from SignalR:", data);
      this.messageHandler.handleStorageUpdate(data);
    });

    this.dataConnection.on("delete", (data: any) => {
      logger.debug("Received delete from SignalR:", data);
      this.messageHandler.handleStorageDelete(data);
    });

    // Handle in-app notification events
    this.dataConnection.on("notificationCreated", (data: any) => {
      logger.debug("Received notificationCreated from SignalR:", data);
      this.messageHandler.handleNotificationCreated(data);
    });

    this.dataConnection.on("notificationArchived", (data: any) => {
      logger.debug("Received notificationArchived from SignalR:", data);
      this.messageHandler.handleNotificationArchived(data);
    });

    this.dataConnection.on("notificationUpdated", (data: any) => {
      logger.debug("Received notificationUpdated from SignalR:", data);
      this.messageHandler.handleNotificationUpdated(data);
    });
  }

  private setupAlarmHubEventHandlers(): void {
    if (!this.alarmConnection) return;

    // Handle connection state changes
    this.alarmConnection.onclose(() => {
      logger.warn("SignalR AlarmHub connection closed");
      // Only trigger reconnect if data connection is also down
      if (!this.dataConnection || this.dataConnection.state !== "Connected") {
        this.handleReconnect();
      }
    });

    this.alarmConnection.onreconnecting(() => {
      logger.info("SignalR AlarmHub connection lost, attempting to reconnect...");
    });

    this.alarmConnection.onreconnected(async () => {
      logger.info("SignalR AlarmHub connection reestablished");
      // Re-subscribe to alarms after reconnection
      await this.subscribeToAlarms();
    });

    // Handle alarm events from AlarmHub
    this.alarmConnection.on("alarm", (alarm: any) => {
      logger.debug("Received alarm from SignalR AlarmHub:", alarm);
      this.messageHandler.handleAlarm(alarm);
    });

    this.alarmConnection.on("urgent_alarm", (alarm: any) => {
      logger.debug("Received urgent_alarm from SignalR AlarmHub:", alarm);
      // Mark as urgent level for the message translator
      this.messageHandler.handleAlarm({ ...alarm, level: "urgent" });
    });

    this.alarmConnection.on("clear_alarm", () => {
      logger.debug("Received clear_alarm from SignalR AlarmHub");
      this.messageHandler.handleClearAlarm();
    });

    this.alarmConnection.on("announcement", (message: any) => {
      logger.debug("Received announcement from SignalR AlarmHub:", message);
      this.messageHandler.handleAnnouncement(message);
    });

    this.alarmConnection.on("notification", (notification: any) => {
      logger.debug("Received notification from SignalR AlarmHub:", notification);
      this.messageHandler.handleNotification(notification);
    });
  }

  private async authenticateWithDataHub(): Promise<void> {
    if (!this.dataConnection) return;
    if (!this.apiSecret) {
      throw new Error(
        "API_SECRET is not configured for the websocket bridge",
      );
    }
    try {
      // Hash the API secret with SHA1 to match Nightscout authentication
      const secretHash = createHash("sha1")
        .update(this.apiSecret)
        .digest("hex")
        .toLowerCase();

      const authData = {
        client: "websocket-bridge",
        secret: secretHash,
        history: 24,
      };

      logger.info("Authenticating with SignalR DataHub...");
      const authResult = await this.dataConnection.invoke("Authorize", authData);

      if (authResult?.success) {
        logger.info("Successfully authenticated with SignalR DataHub");
      } else {
        logger.warn("SignalR DataHub authentication failed:", authResult);
      }
    } catch (error) {
      logger.error("Error authenticating with SignalR DataHub:", error);
    }
  }

  private async subscribeToStorageCollections(): Promise<void> {
    if (!this.dataConnection) return;

    try {
      // Subscribe to all storage collections
      const collections = ["entries", "treatments", "devicestatus", "profiles"];

      logger.info("Subscribing to storage collections:", collections);
      const subscribeResult = await this.dataConnection.invoke("Subscribe", {
        collections: collections,
      });

      if (subscribeResult?.success) {
        logger.info(
          "Successfully subscribed to storage collections:",
          subscribeResult.collections,
        );
      } else {
        logger.warn(
          "Failed to subscribe to some storage collections:",
          subscribeResult,
        );
      }
    } catch (error) {
      logger.error("Error subscribing to storage collections:", error);
    }
  }

  private async subscribeToAlarms(): Promise<void> {
    if (!this.alarmConnection) return;
    if (!this.apiSecret) {
      throw new Error(
        "API_SECRET is not configured for the websocket bridge",
      );
    }

    try {
      // Hash the API secret with SHA1 to match Nightscout authentication
      const secretHash = createHash("sha1")
        .update(this.apiSecret)
        .digest("hex")
        .toLowerCase();

      const subscribeData = {
        Secret: secretHash,
      };

      logger.info("Subscribing to SignalR AlarmHub...");
      const subscribeResult = await this.alarmConnection.invoke("Subscribe", subscribeData);

      if (subscribeResult?.success) {
        logger.info("Successfully subscribed to SignalR AlarmHub");
      } else {
        logger.warn("SignalR AlarmHub subscription failed:", subscribeResult);
      }
    } catch (error) {
      logger.error("Error subscribing to SignalR AlarmHub:", error);
    }
  }

  private async handleReconnect(): Promise<void> {
    if (this.reconnectAttempts >= this.maxReconnectAttempts) {
      logger.error(
        `Maximum reconnection attempts (${this.maxReconnectAttempts}) exceeded`,
      );
      return;
    }

    this.reconnectAttempts++;
    const delay = Math.min(
      this.reconnectDelay * Math.pow(2, this.reconnectAttempts - 1),
      this.maxReconnectDelay,
    );

    logger.info(
      `Attempting to reconnect to SignalR hub in ${delay}ms (attempt ${this.reconnectAttempts}/${this.maxReconnectAttempts})`,
    );

    setTimeout(() => {
      this.connect();
    }, delay);
  }

  async disconnect(): Promise<void> {
    const disconnectPromises: Promise<void>[] = [];

    if (this.dataConnection) {
      disconnectPromises.push(
        this.dataConnection.stop().then(() => {
          logger.info("SignalR DataHub connection stopped");
        }).catch((error) => {
          logger.error("Error stopping SignalR DataHub connection:", error);
        })
      );
    }

    if (this.alarmConnection) {
      disconnectPromises.push(
        this.alarmConnection.stop().then(() => {
          logger.info("SignalR AlarmHub connection stopped");
        }).catch((error) => {
          logger.error("Error stopping SignalR AlarmHub connection:", error);
        })
      );
    }

    await Promise.all(disconnectPromises);
  }

  isConnected(): boolean {
    const dataConnected = this.dataConnection !== null && this.dataConnection.state === "Connected";
    const alarmConnected = this.alarmConnection !== null && this.alarmConnection.state === "Connected";
    return dataConnected && alarmConnected;
  }
}

export default SignalRClient;
