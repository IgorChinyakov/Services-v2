export type PositionDto = {
  id: string;
  name: string;
  description: string | null;
  createdAt: string;
};

export type PositionsCursorPageDto = {
  items: PositionDto[];
  nextCursor: string | null;
};

export type GetPositionsParams = {
  cursor?: string | null;
  size?: number;
  departmentId?: string;
};
