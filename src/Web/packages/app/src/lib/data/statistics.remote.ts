/**
 * Remote functions for statistics calculations
 */
import { getRequestEvent, query } from '$app/server';
import { z } from 'zod';
import { error } from '@sveltejs/kit';
import type { Entry, AveragedStats, TimeInRangeMetrics } from '$lib/api';

const calculateAveragedStatsSchema = z.object({
	entries: z.array(z.object({
		sgv: z.number().optional(),
		mgdl: z.number().optional(),
		date: z.union([z.string(), z.date()]).optional(),
		mills: z.number().optional(),
	})),
});

const calculateTimeInRangeSchema = z.object({
	entries: z.array(z.object({
		sgv: z.number().optional(),
		mgdl: z.number().optional(),
		date: z.union([z.string(), z.date()]).optional(),
		mills: z.number().optional(),
	})),
	config: z.object({
		severeLow: z.number(),
		low: z.number(),
		target: z.number(),
		high: z.number(),
		severeHigh: z.number(),
	}).optional(),
});

/**
 * Calculate averaged stats from entries
 */
export const calculateAveragedStats = query(calculateAveragedStatsSchema, async ({ entries }) => {
	const { locals } = getRequestEvent();
	const { apiClient } = locals;

	try {
		// Transform to Entry[] for the API
		const stats: AveragedStats[] = await apiClient.statistics.calculateAveragedStats(entries as Entry[]);
		return stats;
	} catch (err) {
		console.error('Error calculating averaged stats:', err);
		throw error(500, 'Failed to calculate statistics');
	}
});

/**
 * Calculate time in range metrics from entries
 */
export const calculateTimeInRange = query(calculateTimeInRangeSchema, async ({ entries, config }) => {
	const { locals } = getRequestEvent();
	const { apiClient } = locals;

	try {
		const metrics: TimeInRangeMetrics = await apiClient.statistics.calculateTimeInRange({
			entries: entries as Entry[],
			...(config && { config }),
		});
		return metrics;
	} catch (err) {
		console.error('Error calculating time in range:', err);
		throw error(500, 'Failed to calculate time in range');
	}
});
