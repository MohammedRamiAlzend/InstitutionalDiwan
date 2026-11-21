import { useMemo } from 'react'
import { useQueries } from '@tanstack/react-query'
import { driveItemApi, driveItemKeys, type DriveItemResponse } from '@/entities/drive-item'

/**
 * The caller's effective role for every item on the current page, fetched in one parallel batch
 * (one cached query per item). `undefined` means "still loading" so write actions stay hidden until
 * the role is known, rather than predicting a 403. Drive exposes `/effective`, so gating is accurate.
 */
export function useItemRoles(
  items: readonly DriveItemResponse[],
): Record<string, number | undefined> {
  const queries = useQueries({
    queries: items.map((item) => ({
      queryKey: driveItemKeys.effective(item.itemId),
      queryFn: () => driveItemApi.effective(item.itemId),
      staleTime: 30 * 1000,
    })),
  })

  return useMemo(() => {
    const map: Record<string, number | undefined> = {}
    items.forEach((item, index) => {
      map[item.itemId] = queries[index]?.data?.roleId
    })
    return map
  }, [items, queries])
}